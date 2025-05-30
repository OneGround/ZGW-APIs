using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;
using Roxit.ZGW.Catalogi.Web.Notificaties;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class UpdateInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<UpdateInformatieObjectTypeCommandHandler>,
        IRequestHandler<UpdateInformatieObjectTypeCommand, CommandResult<InformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEntityUpdater<InformatieObjectType> _entityUpdater;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public UpdateInformatieObjectTypeCommandHandler(
        ILogger<UpdateInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IEntityUpdater<InformatieObjectType> entityUpdater,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _informatieObjectTypeDataService = informatieObjectTypeDataService;
    }

    public async Task<CommandResult<InformatieObjectType>> Handle(UpdateInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get InformatieObjectType {Id}....", request.Id);

        var informatieObjectType = await _informatieObjectTypeDataService.GetAsync(
            request.Id,
            cancellationToken,
            trackingChanges: true,
            includeSoftRelations: false
        );
        if (informatieObjectType == null)
        {
            return new CommandResult<InformatieObjectType>(null, CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<InformatieObjectTypeResponseDto>(informatieObjectType);

            var catalogusRsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

            var catalogusId = _uriService.GetId(request.Catalogus);
            var catalogus = await _context
                .Catalogussen.Include(c => c.InformatieObjectTypes)
                .Where(catalogusRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

            if (catalogus == null)
            {
                var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
            }

            var errors = new List<ValidationError>();

            // Check rules on non-concept informatieobjecttype
            ValidatePublishedInformatieObjectTypeChanges(request, informatieObjectType, catalogus, errors);

            if (errors.Count != 0)
            {
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .InformatieObjectTypes.Where(t =>
                            t.Id != informatieObjectType.Id && t.Omschrijving == request.InformatieObjectType.Omschrijving
                        )
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new InformatieObjectType
                    {
                        BeginGeldigheid = request.InformatieObjectType.BeginGeldigheid,
                        EindeGeldigheid = request.InformatieObjectType.EindeGeldigheid,
                        Concept = informatieObjectType.Concept,
                    },
                    errors
                )
            )
            {
                var error = new ValidationError(
                    "informatieobjecttype",
                    ErrorCode.Invalid,
                    $"Informatieobjecttype omschrijving '{request.InformatieObjectType.Omschrijving}' is al gebruikt binnen de geldigheidsperiode."
                );
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
            }

            if (catalogus.Id != informatieObjectType.CatalogusId)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(
                    informatieObjectType.InformatieObjectTypeZaakTypen.Select(z => z.ZaakType),
                    informatieObjectType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(
                    informatieObjectType.InformatieObjectTypeBesluitTypen.Select(z => z.BesluitType),
                    informatieObjectType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(informatieObjectType);

                informatieObjectType.Catalogus = catalogus;
                informatieObjectType.CatalogusId = catalogus.Id;
                informatieObjectType.Owner = informatieObjectType.Catalogus.Owner;
            }

            _logger.LogDebug("Updating InformatieObjectType {Id}....", informatieObjectType.Id);

            _entityUpdater.Update(request.InformatieObjectType, informatieObjectType, version: 1.3M);

            audittrail.SetNew<InformatieObjectTypeResponseDto>(informatieObjectType);

            await _cacheInvalidator.InvalidateAsync(informatieObjectType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(informatieObjectType.Catalogus, informatieObjectType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(informatieObjectType.Catalogus, informatieObjectType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("InformatieObjectType {Id} successfully updated.", informatieObjectType.Id);

        await SendNotificationAsync(Actie.update, informatieObjectType, cancellationToken);

        // Resolve soft BesluitType-InformatieObjectType relations with the current version within geldigheid
        await ResolveBesluitTypeInformatieObjectTypeRelations(informatieObjectType, cancellationToken);

        return new CommandResult<InformatieObjectType>(informatieObjectType, CommandStatus.OK);
    }

    private async Task ResolveBesluitTypeInformatieObjectTypeRelations(InformatieObjectType informatieObjectType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        // Get current valid coupled informatiepobjecttype-besluittypen within geldigheid
        var rsinFilter = GetRsinFilterPredicate<BesluitTypeInformatieObjectType>(b => b.BesluitType.Catalogus.Owner == _rsin);

        var informatieObjectTypeBesluitTypen = await _context
            .BesluitTypeInformatieObjectTypen.Include(b => b.BesluitType)
            .Where(rsinFilter)
            .Where(b => !b.BesluitType.Concept)
            .Where(b => b.BesluitType.CatalogusId == informatieObjectType.CatalogusId)
            .Where(b => now >= b.BesluitType.BeginGeldigheid && (b.BesluitType.EindeGeldigheid == null || now <= b.BesluitType.EindeGeldigheid))
            .Where(b => b.InformatieObjectTypeOmschrijving == informatieObjectType.Omschrijving)
            .ToListAsync(cancellationToken);

        informatieObjectType.InformatieObjectTypeBesluitTypen = informatieObjectTypeBesluitTypen;
    }

    private static void ValidatePublishedInformatieObjectTypeChanges(
        UpdateInformatieObjectTypeCommand request,
        InformatieObjectType informatieObjectType,
        Catalogus catalogus,
        List<ValidationError> errors
    )
    {
        if (informatieObjectType.Concept)
        {
            // Modifications are allowed to make
            return;
        }

        // Be sure catalog is not changed
        if (catalogus.Id != informatieObjectType.CatalogusId)
        {
            var error = new ValidationError(
                "catalogus",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan de catalogus van een gepubliceerd informatieobjecttype aan te passen."
            );
            errors.Add(error);
        }

        // Be sure normal fields are not changed
        if (!informatieObjectType.CanBeUpdated(request.InformatieObjectType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd informatieobjecttype één of meerdere veld(en) te wijzigen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection trefwoord is allowed
        if (!informatieObjectType.Trefwoord.All(t => request.InformatieObjectType.Trefwoord.Contains(t)))
        {
            var error = new ValidationError(
                "trefwoord",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd informatieobjecttype trefwoorden te verwijderen."
            );
            errors.Add(error);
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" };
}

class UpdateInformatieObjectTypeCommand : IRequest<CommandResult<InformatieObjectType>>
{
    public InformatieObjectType InformatieObjectType { get; internal set; }
    public string Catalogus { get; internal set; }
    public Guid Id { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
