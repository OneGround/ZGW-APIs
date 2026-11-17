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

class DeleteCatalogusCommandHandler : CatalogiBaseHandler<DeleteCatalogusCommandHandler>, IRequestHandler<DeleteCatalogusCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly ICatalogEventService _catalogEventService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteCatalogusCommandHandler(
        ILogger<DeleteCatalogusCommandHandler> logger,
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
        _catalogEventService = catalogEventService;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteCatalogusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Catalogus {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogus = await _context.Catalogussen.Where(rsinFilter).SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (catalogus == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        // Note: Also for the forced delete scope: the types are deleted by cascade, which is never what a client of this endpoint wants.
        // No owner filter on the types on purpose: they are scoped by the catalogus, which is already filtered, and a type of another owner
        // (which should not exist) must still block, instead of being deleted by the cascade.
        // Known limitation: the check and the delete are not serialized (no row lock), so a type created in between is deleted by the cascade. The
        // window is tiny and a lock needs provider specific SQL (SELECT ... FOR UPDATE), so this is accepted.
        var hasTypes = await _context
            .Catalogussen.Where(c => c.Id == catalogus.Id)
            .AnyAsync(c => c.ZaakTypes.Any() || c.BesluitTypes.Any() || c.InformatieObjectTypes.Any(), cancellationToken);

        if (hasTypes)
        {
            return new CommandResult(
                CommandStatus.ValidationError,
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.PendingRelations,
                    "De catalogus kan niet worden verwijderd zolang er zaaktypen, besluittypen of informatieobjecttypen in zitten."
                )
            );
        }

        _logger.LogDebug("Deleting Catalogus {Id}....", catalogus.Id);

        // The changes and the event of the host (e.g. a setting in an external store) share a database transaction: when the event throws the database
        // changes are rolled back (what the host already did outside the database is not)
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, legacy: false))
        {
            audittrail.SetOld<CatalogusResponseDto>(catalogus);

            _context.Catalogussen.Remove(catalogus);

            await audittrail.DestroyedAsync(catalogus, catalogus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Catalogus {Id} successfully deleted.", catalogus.Id);

        // Note: A catalogus has no notification channel
        await _catalogEventService.OnCatalogDeletedAsync(catalogus, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new() { Bron = ServiceRoleName.ZTC, Resource = "catalogus" };
}

class DeleteCatalogusCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
