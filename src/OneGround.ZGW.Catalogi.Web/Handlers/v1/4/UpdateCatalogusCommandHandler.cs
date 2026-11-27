using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._4;

class UpdateCatalogusCommandHandler
    : CatalogiBaseHandler<UpdateCatalogusCommandHandler>,
        IRequestHandler<UpdateCatalogusCommand, CommandResult<Catalogus>>
{
    private readonly ZtcDbContext _context;
    private readonly ICatalogEventService _catalogEventService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateCatalogusCommandHandler(
        ILogger<UpdateCatalogusCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICatalogEventService catalogEventService,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _catalogEventService = catalogEventService;
    }

    public async Task<CommandResult<Catalogus>> Handle(UpdateCatalogusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Catalogus {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogus = await _context.Catalogussen.Where(rsinFilter).SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (catalogus == null)
        {
            return new CommandResult<Catalogus>(null, CommandStatus.NotFound);
        }

        var update = request.Catalogus;

        if (update.Rsin != _rsin)
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Invalid,
                $"De Rsin van client_id '{_rsin}' wijkt af van de Rsin in het verzoek '{update.Rsin}'."
            );

            return new CommandResult<Catalogus>(null, CommandStatus.ValidationError, error);
        }

        if (await _context.Catalogussen.Where(rsinFilter).Where(c => c.Id != request.Id).AnyAsync(c => c.Domein == update.Domein, cancellationToken))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Unique,
                $"Er bestaat al een catalogus met dezelfde Rsin '{_rsin}' en Domein '{update.Domein}'."
            );

            return new CommandResult<Catalogus>(null, CommandStatus.ValidationError, error);
        }

        string oldDomein = catalogus.Domein;

        // The changes and the event of the host (e.g. a setting in an external store) share a database transaction: when the event throws the database
        // changes are rolled back (what the host already did outside the database is not)
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, legacy: false))
        {
            audittrail.SetOld<CatalogusResponseDto>(catalogus);

            _logger.LogDebug("Updating Catalogus {Id}....", catalogus.Id);

            catalogus.Domein = update.Domein;
            catalogus.ContactpersoonBeheerNaam = update.ContactpersoonBeheerNaam;
            catalogus.ContactpersoonBeheerTelefoonnummer = update.ContactpersoonBeheerTelefoonnummer;
            catalogus.ContactpersoonBeheerEmailadres = update.ContactpersoonBeheerEmailadres;
            catalogus.Naam = update.Naam;
            catalogus.Versie = update.Versie;
            catalogus.BegindatumVersie = update.BegindatumVersie;

            audittrail.SetNew<CatalogusResponseDto>(catalogus);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(catalogus, catalogus, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(catalogus, catalogus, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Catalogus {Id} successfully updated.", catalogus.Id);

        // Note: Refresh with the types of the catalogus, which were not loaded: the response lists them. A catalogus has no notification channel.
        var refreshed = await _context
            .Catalogussen.AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.ZaakTypes)
            .Include(c => c.BesluitTypes)
            .Include(c => c.InformatieObjectTypes)
            .SingleAsync(c => c.Id == catalogus.Id, cancellationToken);

        // Let the host react on the update, e.g. to keep external settings that are keyed on the domein in sync (it knows the old and new domein)
        await _catalogEventService.OnCatalogUpdatedAsync(oldDomein, catalogus, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CommandResult<Catalogus>(refreshed, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new() { Bron = ServiceRoleName.ZTC, Resource = "catalogus" };
}

class UpdateCatalogusCommand : IRequest<CommandResult<Catalogus>>
{
    public Guid Id { get; set; }
    public Catalogus Catalogus { get; set; }
    public bool IsPartialUpdate { get; set; }
}
