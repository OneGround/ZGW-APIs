using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
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

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

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

        var updatingInformatieObjectType = await _informatieObjectTypeDataService.GetAsync(
            request.Id,
            cancellationToken,
            trackingChanges: true,
            includeSoftRelations: false
        );
        if (updatingInformatieObjectType == null)
        {
            return new CommandResult<InformatieObjectType>(null, CommandStatus.NotFound);
        }

        var validatingInformatieObjectType = await _informatieObjectTypeDataService.GetAsync(
            request.Id,
            cancellationToken,
            trackingChanges: false,
            includeSoftRelations: true
        );
        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConcept(validatingInformatieObjectType, errors))
        {
            return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        foreach (var besluitType in validatingInformatieObjectType.InformatieObjectTypeBesluitTypen.Select(t => t.BesluitType))
        {
            if (!_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
            {
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<InformatieObjectTypeResponseDto>(updatingInformatieObjectType);

            var catalogusRsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

            var catalogusId = _uriService.GetId(request.Catalogus);
            var catalogus = await _context
                .Catalogussen.Include(c => c.InformatieObjectTypes)
                .Where(catalogusRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

            if (catalogus == null)
            {
                var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
                errors.Add(error);
            }

            if (errors.Count != 0)
            {
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .InformatieObjectTypes.Where(t =>
                            t.Id != validatingInformatieObjectType.Id && t.Omschrijving == request.InformatieObjectType.Omschrijving
                        )
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new InformatieObjectType
                    {
                        BeginGeldigheid = request.InformatieObjectType.BeginGeldigheid,
                        EindeGeldigheid = request.InformatieObjectType.EindeGeldigheid,
                        Concept = validatingInformatieObjectType.Concept,
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

            if (catalogus.Id != updatingInformatieObjectType.CatalogusId)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(
                    updatingInformatieObjectType.InformatieObjectTypeZaakTypen.Select(z => z.ZaakType),
                    updatingInformatieObjectType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(
                    updatingInformatieObjectType.InformatieObjectTypeBesluitTypen.Select(z => z.BesluitType),
                    updatingInformatieObjectType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(updatingInformatieObjectType);

                updatingInformatieObjectType.Catalogus = catalogus;
                updatingInformatieObjectType.CatalogusId = catalogus.Id;
                updatingInformatieObjectType.Owner = updatingInformatieObjectType.Catalogus.Owner;
            }

            _logger.LogDebug("Updating InformatieObjectType {Id}....", updatingInformatieObjectType.Id);

            _entityUpdater.Update(request.InformatieObjectType, updatingInformatieObjectType);

            audittrail.SetNew<InformatieObjectTypeResponseDto>(updatingInformatieObjectType);

            await _cacheInvalidator.InvalidateAsync(updatingInformatieObjectType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(updatingInformatieObjectType.Catalogus, updatingInformatieObjectType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(updatingInformatieObjectType.Catalogus, updatingInformatieObjectType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        _logger.LogDebug("InformatieObjectType {Id} successfully updated.", updatingInformatieObjectType.Id);

        await SendNotificationAsync(Actie.update, updatingInformatieObjectType, cancellationToken);

        return new CommandResult<InformatieObjectType>(updatingInformatieObjectType, CommandStatus.OK);
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
