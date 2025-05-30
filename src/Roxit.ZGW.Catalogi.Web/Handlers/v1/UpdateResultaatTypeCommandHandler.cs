using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime.Text;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Referentielijsten.ServiceAgent;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

public class UpdateResultaatTypeCommandHandler
    : CatalogiBaseHandler<UpdateResultaatTypeCommandHandler>,
        IRequestHandler<UpdateResultaatTypeCommand, CommandResult<ResultaatType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IResultaatTypeBusinessRuleService _resultaatTypeBusinessRuleService;
    private readonly IEntityUpdater<ResultaatType> _entityUpdater;
    private readonly IReferentielijstenServiceAgent _referentielijstenServiceAgent;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateResultaatTypeCommandHandler(
        ILogger<UpdateResultaatTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IResultaatTypeBusinessRuleService resultaatTypeBusinessRuleService,
        IEntityUpdater<ResultaatType> entityUpdater,
        IReferentielijstenServiceAgent referentielijstenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _resultaatTypeBusinessRuleService = resultaatTypeBusinessRuleService;
        _entityUpdater = entityUpdater;
        _referentielijstenServiceAgent = referentielijstenServiceAgent;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ResultaatType>> Handle(UpdateResultaatTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ResultType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var resultType = await _context
            .ResultaatTypen.Where(rsinFilter)
            .Include(s => s.ZaakType)
            .ThenInclude(s => s.Catalogus)
            .Include(s => s.BronDatumArchiefProcedure)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (resultType == null)
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(resultType.ZaakType, errors))
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context.ZaakTypen.Where(zaakTypeRsinFilter).SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            errors.Add(new ValidationError("zaaktype", ErrorCode.Invalid, $"ZaakType {request.ZaakType} is onbekend."));
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var resultaat = await _referentielijstenServiceAgent.GetResultaatByUrl(request.ResultaatType.SelectieLijstKlasse);
        if (!resultaat.Success)
        {
            var error = new ValidationError("selectielijstklasse", ErrorCode.InvalidResource, resultaat.Error.Title);
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, error);
        }

        var resultaatTypeOmschrijving = await _referentielijstenServiceAgent.GetResultaatTypeOmschrijvingByUrlAsync(
            request.ResultaatType.ResultaatTypeOmschrijving
        );
        if (!resultaatTypeOmschrijving.Success)
        {
            var error = new ValidationError("resultaattypeomschrijving", ErrorCode.InvalidResource, resultaatTypeOmschrijving.Error.Title);
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, error);
        }

        if (!resultType.ArchiefNominatie.HasValue)
        {
            if (Enum.TryParse<ArchiefNominatie>(resultaat.Response.Waardering, out var archiefNominatie))
            {
                resultType.ArchiefNominatie = archiefNominatie;
            }
        }

        if (resultType.ArchiefActieTermijn == null && resultaat.Response.BewaarTermijn != null)
        {
            resultType.ArchiefActieTermijn = PeriodPattern.NormalizingIso.Parse(resultaat.Response.BewaarTermijn).Value;
        }

        await _resultaatTypeBusinessRuleService.ValidateUpdateAsync(request.ResultaatType, zaakType, resultaat.Response, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating ResultaatType {Id}....", resultType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ResultaatTypeResponseDto>(resultType);

            if (zaakType.Id != resultType.ZaakTypeId)
            {
                await _cacheInvalidator.InvalidateAsync(resultType.ZaakType);

                resultType.ZaakType = zaakType;
                resultType.ZaakTypeId = zaakType.Id;
                resultType.Owner = resultType.ZaakType.Owner;
            }

            _entityUpdater.Update(request.ResultaatType, resultType);

            resultType.OmschrijvingGeneriek = resultaatTypeOmschrijving.Response.Omschrijving;

            audittrail.SetNew<ResultaatTypeResponseDto>(resultType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(resultType.ZaakType, resultType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(resultType.ZaakType, resultType, cancellationToken);
            }

            await _cacheInvalidator.InvalidateAsync(resultType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(resultType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ResultType {Id} successfully updated.", resultType.Id);

        return new CommandResult<ResultaatType>(resultType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "resultaattype" };
}

public class UpdateResultaatTypeCommand : IRequest<CommandResult<ResultaatType>>
{
    public ResultaatType ResultaatType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
