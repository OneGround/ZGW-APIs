using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class UpdateBesluitTypeCommandHandler
    : CatalogiBaseHandler<UpdateBesluitTypeCommandHandler>,
        IRequestHandler<UpdateBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IEntityUpdater<BesluitType> _entityUpdater;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public UpdateBesluitTypeCommandHandler(
        ILogger<UpdateBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IEntityUpdater<BesluitType> entityUpdater,
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _entityUpdater = entityUpdater;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult<BesluitType>> Handle(UpdateBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitType {Id}....", request.Id);

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        if (request.ZaakTypen != null && request.ZaakTypen.Any())
        {
            _logger.LogWarning("Wijzigen van zaaktypen aan een bestaand besluit is niet meer toegestaan. Wijzig de relatie via het zaaktype.");
        }

        var besluitType = await _besluitTypeDataService.GetAsync(request.Id, trackingChanges: true, includeSoftRelations: false, cancellationToken);
        if (besluitType == null)
        {
            return new CommandResult<BesluitType>(null, CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitTypeResponseDto>(besluitType);

            var catalogusRsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

            var catalogusId = _uriService.GetId(request.Catalogus);
            var catalogus = await _context
                .Catalogussen.Include(c => c.BesluitTypes)
                .Where(catalogusRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

            if (catalogus == null)
            {
                var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
            }

            var errors = new List<ValidationError>();

            // Check rules on non-concept besluittype
            ValidatePublishedBesluitTypeChanges(request, besluitType, catalogus, errors);

            if (errors.Count != 0)
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .BesluitTypes.Where(t => t.Id != besluitType.Id && t.Omschrijving == request.BesluitType.Omschrijving)
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new BesluitType
                    {
                        BeginGeldigheid = request.BesluitType.BeginGeldigheid,
                        EindeGeldigheid = request.BesluitType.EindeGeldigheid,
                        Concept = besluitType.Concept,
                    },
                    errors
                )
            )
            {
                var error = new ValidationError(
                    "besluittype",
                    ErrorCode.Invalid,
                    $"Besluittype omschrijving '{request.BesluitType.Omschrijving}' is al gebruikt binnen de geldigheidsperiode."
                );
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
            }

            if (catalogus.Id != besluitType.CatalogusId)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(
                    besluitType.BesluitTypeInformatieObjectTypen.Select(z => z.InformatieObjectType),
                    besluitType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(besluitType);

                besluitType.Catalogus = catalogus;
                besluitType.CatalogusId = catalogus.Id;
                besluitType.Owner = catalogus.Owner;
            }

            // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
            //await UpdateZaakTypen(request, besluitType, cancellationToken);
            await UpdateInformatieObjectTypen(request, besluitType, cancellationToken);

            if (errors.Count != 0)
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            _logger.LogDebug("Updating BesluitType {Id}....", besluitType.Id);

            _entityUpdater.Update(request.BesluitType, besluitType, version: 1.3M);

            audittrail.SetNew<BesluitTypeResponseDto>(besluitType);

            await _cacheInvalidator.InvalidateAsync(besluitType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(besluitType.Catalogus, besluitType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(besluitType.Catalogus, besluitType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("BesluitType {Id} successfully updated.", besluitType.Id);

        // Note: Refresh created BesluitType with all sub-entities within geldigheid which was not loaded
        besluitType = await _besluitTypeDataService.GetAsync(besluitType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.update, besluitType, cancellationToken);

        return new CommandResult<BesluitType>(besluitType, CommandStatus.OK);
    }

    private static void ValidatePublishedBesluitTypeChanges(
        UpdateBesluitTypeCommand request,
        BesluitType besluitType,
        Catalogus catalogus,
        List<ValidationError> errors
    )
    {
        if (besluitType.Concept)
        {
            // Modifications are allowed to make
            return;
        }

        // Be sure catalog is not changed
        if (catalogus.Id != besluitType.CatalogusId)
        {
            var error = new ValidationError(
                "catalogus",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan de catalogus van een gepubliceerd besluittype aan te passen."
            );
            errors.Add(error);
        }

        // Be sure normal fields are not changed
        if (!besluitType.CanBeUpdated(request.BesluitType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd besluittype één of meerdere veld(en) te wijzigen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection informatieobjecttypen is allowed
        if (
            !besluitType
                .BesluitTypeInformatieObjectTypen.Select(z => z.InformatieObjectTypeOmschrijving)
                .All(t => request.InformatieObjectTypen.Contains(t))
        )
        {
            var error = new ValidationError(
                "informatieobjecttypen",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd besluittype informatieobjecttypen te verwijderen."
            );
            errors.Add(error);
        }
    }

    private async Task UpdateInformatieObjectTypen(UpdateBesluitTypeCommand request, BesluitType besluitType, CancellationToken cancellationToken)
    {
        // Get the old besluittype id's to invalidate cache later
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var informatieObjectTypeFilter = GetRsinFilterPredicate<InformatieObjectType>(t => t.Catalogus.Owner == _rsin);
        var informatieObjectTypen = new List<BesluitTypeInformatieObjectType>();

        foreach (var (informatieObjectType, index) in request.InformatieObjectTypen.WithIndex())
        {
            var informatieObjectTypenWithinGeldigheid = await _context
                .InformatieObjectTypen.Include(b => b.Catalogus)
                .Where(informatieObjectTypeFilter)
                .Where(b => b.CatalogusId == _uriService.GetId(request.Catalogus))
                .Where(b =>
                    b.Omschrijving == informatieObjectType && now >= b.BeginGeldigheid && (b.EindeGeldigheid == null || now <= b.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            if (informatieObjectTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation(
                    "Waarschuwing: informatieobjecttypen.{index}.omschrijving. InformatieObjectType {informatieObjectType} is onbekend.",
                    index,
                    informatieObjectType
                );
                continue;
            }

            informatieObjectTypen.AddRangeUnique(
                informatieObjectTypenWithinGeldigheid.Select(b => new BesluitTypeInformatieObjectType
                {
                    BesluitType = besluitType,
                    InformatieObjectTypeOmschrijving = b.Omschrijving,
                    Owner = besluitType.Owner,
                    InformatieObjectType = b,
                }),
                (x, y) => x.InformatieObjectTypeOmschrijving == y.InformatieObjectTypeOmschrijving
            );
        }

        besluitType.BesluitTypeInformatieObjectTypen.Clear();

        _context.BesluitTypeInformatieObjectTypen.AddRange(informatieObjectTypen);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

public class UpdateBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public BesluitType BesluitType { get; internal set; }
    public Guid Id { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> ZaakTypen { get; internal set; }
    public IEnumerable<string> InformatieObjectTypen { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
