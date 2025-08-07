using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Referentielijsten.ServiceAgent;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class CreateZaakTypeCommandHandler
    : CatalogiBaseHandler<CreateZaakTypeCommandHandler>,
        IRequestHandler<CreateZaakTypeCommand, CommandResult<ZaakType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IReferentielijstenServiceAgent _referentielijstenServiceAgent;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public CreateZaakTypeCommandHandler(
        ILogger<CreateZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IReferentielijstenServiceAgent referentielijstenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _referentielijstenServiceAgent = referentielijstenServiceAgent;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult<ZaakType>> Handle(CreateZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakType and validating....");

        var zaakType = request.ZaakType;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogusId = _uriService.GetId(request.Catalogus);
        var catalogus = await _context.Catalogussen.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

        if (catalogus == null)
        {
            var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
            return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
        }

        zaakType.Catalogus = catalogus;
        zaakType.Owner = zaakType.Catalogus.Owner;
        if (zaakType.ReferentieProces != null)
        {
            zaakType.ReferentieProces.Owner = zaakType.Owner;
        }

        if (request.ZaakType.SelectielijstProcestype != null)
        {
            var procesTypeResult = await _referentielijstenServiceAgent.GetProcesTypeByUrlAsync(request.ZaakType.SelectielijstProcestype);
            if (!procesTypeResult.Success)
            {
                var error = new ValidationError("selectielijstProcestype", ErrorCode.InvalidResource, procesTypeResult.Error.Title);
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
            }
        }

        await AddDeelZaakTypen(request, zaakType, errors, cancellationToken);
        await AddGerelateerdeZaakTypen(request, zaakType, errors, cancellationToken);
        await AddBesluitTypen(request, zaakType, errors, cancellationToken);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.ZaakTypen.AddAsync(zaakType, cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ZaakTypeResponseDto>(zaakType);

            await audittrail.CreatedAsync(zaakType.Catalogus, zaakType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully created.", zaakType.Id);

        // Note: Refresh created ZaakType with all sub-entities within geldigheid which was not loaded
        zaakType = await _zaakTypeDataService.GetAsync(zaakType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.create, zaakType, cancellationToken);

        await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType), zaakType.Catalogus.Owner);

        return new CommandResult<ZaakType>(zaakType, CommandStatus.OK);
    }

    private async Task AddBesluitTypen(
        CreateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        var besluitTypeFilter = GetRsinFilterPredicate<BesluitType>(t => t.Catalogus.Owner == _rsin);
        var besluitTypen = new List<ZaakTypeBesluitType>();

        foreach (var (url, index) in request.BesluitTypen.WithIndex())
        {
            var besluitType = await _context
                .BesluitTypen.Include(b => b.Catalogus)
                .Where(besluitTypeFilter)
                .SingleOrDefaultAsync(b => b.Id == _uriService.GetId(url), cancellationToken);

            if (besluitType == null)
            {
                var error = new ValidationError($"besluittypen.{index}.url", ErrorCode.Invalid, $"BesluitType {url} is onbekend.");
                errors.Add(error);
            }
            else if (besluitType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{besluitType.Catalogus.Id} moeten tot dezelfde catalogus behoren als het BESLUITTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
            {
                besluitTypen.AddUnique(
                    new ZaakTypeBesluitType
                    {
                        ZaakType = zaakType,
                        BesluitTypeOmschrijving = besluitType.Omschrijving,
                        Owner = zaakType.Owner,
                        BesluitType = besluitType,
                    },
                    (x, y) => x.BesluitTypeOmschrijving == y.BesluitTypeOmschrijving
                );
            }
        }

        if (errors.Count != 0)
        {
            return;
        }

        _context.ZaakTypeBesluitTypen.AddRange(besluitTypen);

        await _cacheInvalidator.InvalidateAsync(besluitTypen.Select(t => t.BesluitType), zaakType.Catalogus.Owner);
    }

    private async Task AddGerelateerdeZaakTypen(
        CreateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);

        foreach (var (ztgzt, index) in zaakType.ZaakTypeGerelateerdeZaakTypen.WithIndex())
        {
            var gerelateerdeZaakType = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeFilter)
                // Note: In 1.3 GerelateerdeZaakTypeIdentificatie contains the Zaaktype Identificatie for matching but for 1.0 the url is mapped into
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(ztgzt.GerelateerdeZaakTypeIdentificatie), cancellationToken);
            if (gerelateerdeZaakType == null)
            {
                var error = new ValidationError($"gerelateerdezaaktypen.{index}.url", ErrorCode.Invalid, $"ZaakType {ztgzt} is onbekend.");
                errors.Add(error);
            }
            else if (gerelateerdeZaakType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{gerelateerdeZaakType.Catalogus.Id} moeten tot dezelfde catalogus behoren als het ZAAKTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(gerelateerdeZaakType, errors, version: 1.0M))
            {
                ztgzt.GerelateerdeZaakTypeIdentificatie = gerelateerdeZaakType.Identificatie;
                ztgzt.GerelateerdeZaakType = gerelateerdeZaakType;
                ztgzt.Owner = zaakType.Owner;
            }
        }

        if (errors.Count != 0)
        {
            return;
        }

        await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeGerelateerdeZaakTypen.Select(z => z.GerelateerdeZaakType), zaakType.Catalogus.Owner);
    }

    private async Task AddDeelZaakTypen(
        CreateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var deelZaakTypen = new List<ZaakTypeDeelZaakType>();

        foreach (var (url, index) in request.DeelZaakTypen.WithIndex())
        {
            var deelZaakType = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(url), cancellationToken);
            if (deelZaakType == null)
            {
                var error = new ValidationError($"deelzaaktypen.{index}.url", ErrorCode.Invalid, $"ZaakType {url} is onbekend.");
                errors.Add(error);
            }
            else if (deelZaakType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{deelZaakType.Catalogus.Id} moeten tot dezelfde catalogus behoren als het ZAAKTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(deelZaakType, errors, version: 1.0M))
            {
                deelZaakTypen.AddUnique(
                    new ZaakTypeDeelZaakType
                    {
                        ZaakType = zaakType,
                        DeelZaakTypeIdentificatie = deelZaakType.Identificatie,
                        DeelZaakType = deelZaakType,
                        Owner = zaakType.Owner,
                    },
                    (x, y) => x.DeelZaakTypeIdentificatie == y.DeelZaakTypeIdentificatie
                );
            }
        }

        if (errors.Count != 0)
        {
            return;
        }

        _context.ZaakTypeDeelZaakTypen.AddRange(deelZaakTypen);

        await _cacheInvalidator.InvalidateAsync(deelZaakTypen.Select(t => t.DeelZaakType), zaakType.Catalogus.Owner);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class CreateZaakTypeCommand : IRequest<CommandResult<ZaakType>>
{
    public ZaakType ZaakType { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> DeelZaakTypen { get; internal set; }
    public IEnumerable<string> BesluitTypen { get; internal set; }
}
