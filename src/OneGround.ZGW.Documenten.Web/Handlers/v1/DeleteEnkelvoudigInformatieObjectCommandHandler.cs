using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class DeleteEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<DeleteEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<DeleteEnkelvoudigInformatieObjectCommand, CommandResult>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IDocumentServicesResolver _documentServicesResolver;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;

    public DeleteEnkelvoudigInformatieObjectCommandHandler(
        ILogger<DeleteEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        DrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IDocumentServicesResolver documentServicesResolver,
        IZakenServiceAgent zakenServiceAgent,
        IBesluitenServiceAgent besluitenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _documentServicesResolver = documentServicesResolver;
        _zakenServiceAgent = zakenServiceAgent;
        _besluitenServiceAgent = besluitenServiceAgent;
    }

    public async Task<CommandResult> Handle(DeleteEnkelvoudigInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get EnkelvoudigInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        // Use ReadCommitted isolation level:
        // - FOR UPDATE provides pessimistic row-level locking (prevents concurrent modifications)
        // - xmin (configured in EnkelvoudigInformatieObject) provides optimistic concurrency detection (detects any changes since read)
        // - ReadCommitted allows better concurrency than Serializable for this use case
        // - The combination prevents both lost updates (via FOR UPDATE) and write skew (via xmin)
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // First, try to acquire lock on the EnkelvoudigInformatieObject
        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [request.Id])
            .Include(x => x.ObjectInformatieObjecten)
            .Include(x => x.EnkelvoudigInformatieObjectVersies)
            .Include(x => x.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            // The object might be locked OR not exist - check if it exists without lock
            var exists = await _context.EnkelvoudigInformatieObjecten.AnyAsync(e => e.Id == request.Id, cancellationToken);

            if (!exists)
            {
                // Object truly doesn't exist
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.NotFound);
            }

            // Object exists but is locked by another process
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Conflict,
                $"Het enkelvoudiginformatieobject {request.Id} is vergrendeld door een andere bewerking."
            );

            return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Conflict, error);
        }

        // NOTE: Vernietigen van informatieobjecten (drc-008) => Een EnkelvoudigInformatieObject MAG ALLEEN verwijderd worden indien er geen ObjectInformatieObject-en meer aan hangen.
        // Indien er nog relaties zijn, dan MOET het DRC antwoorden met een HTTP 400 foutbericht
        if (enkelvoudigInformatieObject.ObjectInformatieObjecten.Any())
        {
            // Because relations are stored both in DRC and in ZRC/BRC be sure there is an external relation (ZRC and/or BRC).
            // If not we could ignore the validation error here
            if (await IsReallyReferencedByExternalComponents(_uriService.GetUri(enkelvoudigInformatieObject)))
            {
                return new CommandResult(
                    CommandStatus.ValidationError,
                    [
                        new ValidationError(
                            "nonFieldErrors",
                            ErrorCode.PendingRelations,
                            $"EnkelvoudigInformatieObject {enkelvoudigInformatieObject.Id} is gerefereerd aan één of meerdere ObjectInformatieObjecten."
                        ),
                    ]
                );
            }
        }

        var documentUrns = enkelvoudigInformatieObject
            .EnkelvoudigInformatieObjectVersies.Where(d => d.Inhoud != null) // Note: Is changed in v1.1 (so can be null)
            .Select(d => d.Inhoud)
            .Distinct()
            .ToList();

        // 1. Delete the metadata
        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting EnkelvoudigInformatieObject {Id}....", enkelvoudigInformatieObject.Id);

            await _context
                .EnkelvoudigInformatieObjecten.Where(x => x.Id == enkelvoudigInformatieObject.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LatestEnkelvoudigInformatieObjectVersieId, y => null), cancellationToken);

            // Remove the audittrail for this EnkelvoudigInformatieObjecten first
            await _context
                .AuditTrailRegels.Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == enkelvoudigInformatieObject.Id)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.EnkelvoudigInformatieObjecten.Where(x => x.Id == enkelvoudigInformatieObject.Id).ExecuteDeleteAsync(cancellationToken);

            var latestEnkelvoudigInformatieObjectVersieIdentificatie = enkelvoudigInformatieObject
                .LatestEnkelvoudigInformatieObjectVersie
                ?.Identificatie;

            await audittrail.DestroyedAsync(
                enkelvoudigInformatieObject,
                toelichting: $"Betreft enkelvoudiginformatieobject met identificatie {latestEnkelvoudigInformatieObjectVersieIdentificatie}",
                cancellationToken
            );

            await tx.CommitAsync(cancellationToken);

            _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully deleted.", enkelvoudigInformatieObject.Id);
        }

        // 2. Delete all document versions (content) from DMS
        _logger.LogDebug("Deleting documents from DMS....");
        foreach (var document in documentUrns)
        {
            var documentUrn = new DocumentUrn(document);

            var service = _documentServicesResolver.Find(documentUrn.Type);
            if (service == null)
            {
                _logger.LogError("Could not find a document provider to handle {documentUrn}.", documentUrn);
                continue; // Note: Continue deleting documents from another DMS
            }

            try
            {
                await service.DeleteDocumentAsync(documentUrn, cancellationToken);

                _logger.LogDebug("Document '{documentUrn}' deleted from DMS [{ProviderPrefix}]", documentUrn, service.ProviderPrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document '{documentUrn}' from DMS [{ProviderPrefix}].", documentUrn, service.ProviderPrefix);
                // Note: Swallow the exception (only log error)
            }
        }

        // 3. Notify...
        await SendNotificationAsync(Actie.destroy, enkelvoudigInformatieObject, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private async Task<bool> IsReallyReferencedByExternalComponents(string enkelvoudigInformatieObject)
    {
        var taskZrc = _zakenServiceAgent.GetZaakInformatieObjectenAsync(
            new GetAllZaakInformatieObjectenQueryParameters { InformatieObject = enkelvoudigInformatieObject }
        );

        var taskBrc = _besluitenServiceAgent.GetBesluitInformatieObjectenAsync(
            new GetAllBesluitInformatieObjectenQueryParameters { InformatieObject = enkelvoudigInformatieObject }
        );

        await Task.WhenAll(taskZrc, taskBrc);

        if (!taskZrc.Result.Success || !taskBrc.Result.Success)
            return true; // In case of any error return true to keep eventually stored references!!

        var hasReferences = taskZrc.Result.Response.Any() || taskBrc.Result.Response.Any();

        return hasReferences;
    }

    private static AuditTrailOptions AuditTrailOptions => new() { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

class DeleteEnkelvoudigInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
