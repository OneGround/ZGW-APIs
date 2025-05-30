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
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Referentielijsten.ServiceAgent;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class CreateResultaatTypeCommandHandler
    : CatalogiBaseHandler<CreateResultaatTypeCommandHandler>,
        IRequestHandler<CreateResultaatTypeCommand, CommandResult<ResultaatType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IResultaatTypeBusinessRuleService _resultaatTypeBusinessRuleService;
    private readonly IReferentielijstenServiceAgent _referentielijstenServiceAgent;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateResultaatTypeCommandHandler(
        ILogger<CreateResultaatTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IResultaatTypeBusinessRuleService resultaatTypeBusinessRuleService,
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
        _referentielijstenServiceAgent = referentielijstenServiceAgent;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ResultaatType>> Handle(CreateResultaatTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ResultType and validating....");

        var resultType = request.ResultaatType;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, error);
        }

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var resultaat = await _referentielijstenServiceAgent.GetResultaatByUrl(resultType.SelectieLijstKlasse);
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

        await _resultaatTypeBusinessRuleService.ValidateAsync(resultType, zaakType, resultaat.Response, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.ResultaatTypen.AddAsync(resultType, cancellationToken);

        resultType.OmschrijvingGeneriek = resultaatTypeOmschrijving.Response.Omschrijving;
        resultType.ZaakType = zaakType;
        resultType.Owner = resultType.ZaakType.Owner;

        await _cacheInvalidator.InvalidateAsync(resultType.ZaakType);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ResultaatTypeResponseDto>(resultType);

            await audittrail.CreatedAsync(resultType.ZaakType, resultType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ResultaatType {Id} successfully created.", resultType.Id);

        return new CommandResult<ResultaatType>(resultType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "resultaattype" };
}

class CreateResultaatTypeCommand : IRequest<CommandResult<ResultaatType>>
{
    public ResultaatType ResultaatType { get; internal set; }
    public string ZaakType { get; internal set; }
}
