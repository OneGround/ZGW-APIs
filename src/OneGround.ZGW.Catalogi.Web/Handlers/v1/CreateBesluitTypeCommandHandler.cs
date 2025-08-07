using System;
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

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class CreateBesluitTypeCommandHandler
    : CatalogiBaseHandler<CreateBesluitTypeCommandHandler>,
        IRequestHandler<CreateBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public CreateBesluitTypeCommandHandler(
        ILogger<CreateBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult<BesluitType>> Handle(CreateBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating BesluitType and validating....");

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        if (request.ZaakTypen != null && request.ZaakTypen.Any())
        {
            var error = new ValidationError(
                "zaaktypen",
                ErrorCode.Invalid,
                "Toevoegen van zaaktypen aan een besluit is niet meer toegestaan. Maak de relatie aan via het zaaktype."
            );
            return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
        }

        var besluitType = request.BesluitType;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogusId = _uriService.GetId(request.Catalogus);
        var catalogus = await _context.Catalogussen.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

        if (catalogus == null)
        {
            var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
            errors.Add(error);
        }
        else
        {
            besluitType.Catalogus = catalogus;
            besluitType.Owner = besluitType.Catalogus.Owner;
        }

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        //await AddZaakTypen(request, besluitType, errors, catalogusId, cancellationToken);
        await AddInformatieObjectTypen(request, besluitType, errors, catalogusId, cancellationToken);

        if (errors.Count != 0)
        {
            return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.BesluitTypen.AddAsync(besluitType, cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<BesluitTypeResponseDto>(besluitType);

            await audittrail.CreatedAsync(besluitType.Catalogus, besluitType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("BesluitType {Id} successfully created.", besluitType.Id);

        // Note: Refresh created BesluitType with all sub-entities within geldigheid which was not loaded
        besluitType = await _besluitTypeDataService.GetAsync(besluitType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.create, besluitType, cancellationToken);

        await _cacheInvalidator.InvalidateAsync(besluitType.BesluitTypeZaakTypen.Select(t => t.ZaakType), besluitType.Catalogus.Owner);
        await _cacheInvalidator.InvalidateAsync(besluitType.BesluitTypeResultaatTypen.Select(t => t.ResultaatType), besluitType.Catalogus.Owner);

        return new CommandResult<BesluitType>(besluitType, CommandStatus.OK);
    }

    private async Task AddInformatieObjectTypen(
        CreateBesluitTypeCommand request,
        BesluitType besluitType,
        List<ValidationError> errors,
        Guid catalogusId,
        CancellationToken cancellationToken
    )
    {
        var informatieObjectTypeRsinFilter = GetRsinFilterPredicate<InformatieObjectType>(t => t.Catalogus.Owner == _rsin);
        var informatieObjectTypen = new List<BesluitTypeInformatieObjectType>();

        foreach (var (url, index) in request.InformatieObjectTypen.WithIndex())
        {
            var informatieObjectType = await _context
                .InformatieObjectTypen.Include(z => z.Catalogus)
                .Where(informatieObjectTypeRsinFilter)
                .SingleOrDefaultAsync(b => b.Id == _uriService.GetId(url), cancellationToken);

            if (informatieObjectType == null)
            {
                var error = new ValidationError($"informatieobjecttypen.{index}.url", ErrorCode.Invalid, $"InformatieObjectType {url} is onbekend.");
                errors.Add(error);
            }
            else if (informatieObjectType.Catalogus.Id != catalogusId)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"Catalogus {request.Catalogus} is onbekend."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
            {
                informatieObjectTypen.AddUnique(
                    new BesluitTypeInformatieObjectType
                    {
                        BesluitType = besluitType,
                        InformatieObjectTypeOmschrijving = informatieObjectType.Omschrijving,
                        Owner = besluitType.Owner,
                        InformatieObjectType = informatieObjectType,
                    },
                    (x, y) => x.InformatieObjectTypeOmschrijving == y.InformatieObjectTypeOmschrijving
                );
            }
        }

        if (errors.Count != 0)
        {
            return;
        }

        _context.BesluitTypeInformatieObjectTypen.AddRange(informatieObjectTypen);

        await _cacheInvalidator.InvalidateAsync(informatieObjectTypen.Select(t => t.InformatieObjectType), besluitType.Catalogus.Owner);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

class CreateBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public BesluitType BesluitType { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> ZaakTypen { get; internal set; }
    public IEnumerable<string> InformatieObjectTypen { get; internal set; }
}
