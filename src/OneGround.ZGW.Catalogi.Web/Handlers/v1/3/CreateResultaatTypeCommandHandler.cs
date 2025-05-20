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
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Referentielijsten.ServiceAgent;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

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
            .ZaakTypen.Include(z => z.Catalogus)
            .Where(rsinFilter)
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

        resultType.OmschrijvingGeneriek = resultaatTypeOmschrijving.Response.Omschrijving;
        resultType.ZaakType = zaakType;
        resultType.Owner = resultType.ZaakType.Owner;
        // Note: Derive from Zaaktype instead of getting from request (decided to do so)
        resultType.BeginGeldigheid = zaakType.BeginGeldigheid;
        resultType.EindeGeldigheid = zaakType.EindeGeldigheid;
        resultType.BeginObject = zaakType.BeginObject;
        resultType.EindeObject = zaakType.EindeObject;
        // ----

        await AddBesluitTypen(request, resultType, zaakType.CatalogusId, cancellationToken);

        await _context.ResultaatTypen.AddAsync(resultType, cancellationToken);

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

    private async Task AddBesluitTypen(
        CreateResultaatTypeCommand request,
        ResultaatType resultaatType,
        Guid catalogusId,
        CancellationToken cancellationToken
    )
    {
        var besluitTypeFilter = GetRsinFilterPredicate<BesluitType>(t => t.Catalogus.Owner == _rsin);
        var besluitTypen = new List<ResultaatTypeBesluitType>();

        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        foreach (var (besluitType, index) in request.BesluitTypen.WithIndex())
        {
            var besluitTypenWithinGeldigheid = await _context
                .BesluitTypen.Include(b => b.Catalogus)
                .Where(besluitTypeFilter)
                .Where(b => b.CatalogusId == catalogusId)
                .Where(b => b.Omschrijving == besluitType && now >= b.BeginGeldigheid && (b.EindeGeldigheid == null || now <= b.EindeGeldigheid))
                .ToListAsync(cancellationToken);

            if (besluitTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation("Waarschuwing: besluittypen.{index}.omschrijving. BesluitType {besluitType} is onbekend.", index, besluitType);
                continue;
            }

            besluitTypen.AddRangeUnique(
                besluitTypenWithinGeldigheid.Select(b => new ResultaatTypeBesluitType
                {
                    ResultaatType = resultaatType,
                    BesluitTypeOmschrijving = b.Omschrijving,
                    Owner = resultaatType.Owner,
                    BesluitType = b,
                }),
                (x, y) => x.BesluitTypeOmschrijving == y.BesluitTypeOmschrijving
            );
        }

        _context.ResultaatTypeBesluitTypen.AddRange(besluitTypen);

        await _cacheInvalidator.InvalidateAsync(besluitTypen.Select(t => t.BesluitType), resultaatType.Owner);
    }
}

class CreateResultaatTypeCommand : IRequest<CommandResult<ResultaatType>>
{
    public ResultaatType ResultaatType { get; internal set; }
    public string ZaakType { get; internal set; }
    public IEnumerable<string> BesluitTypen { get; internal set; }
}
