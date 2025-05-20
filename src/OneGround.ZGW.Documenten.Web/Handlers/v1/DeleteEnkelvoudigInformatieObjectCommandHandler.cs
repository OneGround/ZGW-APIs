using System;
using System.Collections.Generic;
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
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
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

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.Where(rsinFilter)
            .Include(z => z.EnkelvoudigInformatieObjectVersies)
            .Include(z => z.ObjectInformatieObjecten)
            .AsSplitQuery()
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var documentUrns = enkelvoudigInformatieObject
            .EnkelvoudigInformatieObjectVersies.Where(d => d.Inhoud != null) // Note: Is changed in v1.1 (so can be null)
            .Select(d => d.Inhoud)
            .Distinct()
            .ToList();

        var errors = new List<ValidationError>();

        // Note: Vernietigen van informatieobjecten (drc-008) => Een EnkelvoudigInformatieObject MAG ALLEEN verwijderd worden indien er geen ObjectInformatieObject-en meer aan hangen.Indien er nog relaties zijn, dan MOET het DRC antwoorden met een HTTP 400 foutbericht
        if (enkelvoudigInformatieObject.ObjectInformatieObjecten.Count != 0)
        {
            // Because relations are stored both in DRC and in ZRC/BRC be sure there is a external reation (ZRC and/or BRC). If not we could ignore the validation error here
            if (IsReallyReferencedByExternalComponents(_uriService.GetUri(enkelvoudigInformatieObject)))
            {
                errors.Add(
                    new ValidationError(
                        "nonFieldErrors",
                        ErrorCode.PendingRelations,
                        $"EnkelvoudigInformatieObject {enkelvoudigInformatieObject.Id} is gerefereerd aan één of meerdere ObjectInformatieObjecten."
                    )
                );
            }
        }
        if (errors.Count != 0)
        {
            return new CommandResult(CommandStatus.ValidationError, errors.ToArray());
        }

        //
        // 1. Delete the metadata

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting EnkelvoudigInformatieObject {Id}....", enkelvoudigInformatieObject.Id);

            using (var trans = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                enkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersieId = null;

                await _context.SaveChangesAsync(cancellationToken);

                // Remove the audittrail for this EnkelvoudigInformatieObjecten first
                var audittrailregels = _context.AuditTrailRegels.Where(a =>
                    a.HoofdObjectId.HasValue && a.HoofdObjectId == enkelvoudigInformatieObject.Id
                );

                _context.AuditTrailRegels.RemoveRange(audittrailregels);

                _context.Remove(enkelvoudigInformatieObject);

                // Audittrail: Record that only the enkelvoudigInformatieObject has been deleted WITHOUT any details expect the zaak identification (field toelichting)!!
                var latestIdentificatie = enkelvoudigInformatieObject
                    .EnkelvoudigInformatieObjectVersies.OrderBy(a => a.Versie)
                    .LastOrDefault()
                    ?.Identificatie; // Should always have at least one version

                await audittrail.DestroyedAsync(
                    enkelvoudigInformatieObject,
                    toelichting: $"Betreft enkelvoudiginformatieobject met identificatie {latestIdentificatie}",
                    cancellationToken
                );

                await _context.SaveChangesAsync(cancellationToken);

                await trans.CommitAsync(cancellationToken);
            }
            _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully deleted.", enkelvoudigInformatieObject.Id);
        }

        //
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

        //
        // 3. Notify...

        await SendNotificationAsync(Actie.destroy, enkelvoudigInformatieObject, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private bool IsReallyReferencedByExternalComponents(string enkelvoudigInformatieObject)
    {
        var taskZrc = _zakenServiceAgent.GetZaakInformatieObjectenAsync(
            new GetAllZaakInformatieObjectenQueryParameters { InformatieObject = enkelvoudigInformatieObject }
        );

        var taskBrc = _besluitenServiceAgent.GetBesluitInformatieObjectenAsync(
            new GetAllBesluitInformatieObjectenQueryParameters { InformatieObject = enkelvoudigInformatieObject }
        );

        Task.WaitAll(taskZrc, taskBrc);

        if (!taskZrc.Result.Success || !taskBrc.Result.Success)
            return true; // In case of any error return true to keep eventually stored references!!

        var hasReferences = taskZrc.Result.Response.Any() || taskBrc.Result.Response.Any();

        return hasReferences;
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

class DeleteEnkelvoudigInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
