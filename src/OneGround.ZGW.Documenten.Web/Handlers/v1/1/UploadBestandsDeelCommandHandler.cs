using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._1;

class UploadBestandsDeelCommandHandler
    : DocumentenBaseHandler<UploadBestandsDeelCommandHandler>,
        IRequestHandler<UploadBestandsDeelCommand, CommandResult<BestandsDeel>>
{
    private readonly DrcDbContext _context;
    private readonly IDocumentService _documentService;
    private readonly IFileValidationService _fileValidationService;

    public UploadBestandsDeelCommandHandler(
        ILogger<UploadBestandsDeelCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentServicesResolver documentServicesResolver,
        INotificatieService notificatieService,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IFileValidationService fileValidationService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _documentService = documentServicesResolver.GetDefault();
        _fileValidationService = fileValidationService;
    }

    public async Task<CommandResult<BestandsDeel>> Handle(UploadBestandsDeelCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Uploading bestandsdeel...");

        var rsinFilter = GetRsinFilterPredicate<BestandsDeel>(o => o.EnkelvoudigInformatieObjectVersie.InformatieObject.Owner == _rsin);

        var bestandsdeel = await _context
            .BestandsDelen.Where(rsinFilter)
            .Include(d => d.EnkelvoudigInformatieObjectVersie)
            .ThenInclude(d => d.InformatieObject)
            .SingleOrDefaultAsync(d => d.Id == request.BestandsDeelId, cancellationToken);

        if (bestandsdeel == null)
        {
            return new CommandResult<BestandsDeel>(null, CommandStatus.NotFound);
        }

        if (bestandsdeel.Voltooid)
        {
            return new CommandResult<BestandsDeel>(bestandsdeel, CommandStatus.OK);
        }

        if (string.IsNullOrEmpty(request.Lock) || request.Lock != bestandsdeel.EnkelvoudigInformatieObjectVersie.InformatieObject.Lock)
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.IncorrectLockId, "Incorrect lock ID.");
            return new CommandResult<BestandsDeel>(null, CommandStatus.ValidationError, error);
        }

        var versie = await _context
            .EnkelvoudigInformatieObjectVersies.Include(b => b.BestandsDelen)
            .OrderBy(b => b.Versie)
            .LastOrDefaultAsync(
                b => b.EnkelvoudigInformatieObjectId == bestandsdeel.EnkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObjectId,
                cancellationToken
            );

        IMultiPartDocument multipartdocument = new MultiPartDocument(bestandsdeel.EnkelvoudigInformatieObjectVersie.MultiPartDocumentId);

        try
        {
            // Upload bestandsdeel
            await using (var stream = request.Inhoud.OpenReadStream())
            {
                await _fileValidationService.ValidateAsync(stream, bestandsdeel.EnkelvoudigInformatieObjectVersie.Formaat, cancellationToken);

                var result = await _documentService.TryUploadPartAsync(
                    multipartdocument,
                    stream,
                    bestandsdeel.Volgnummer,
                    bestandsdeel.Omvang,
                    cancellationToken
                );

                if (result == null)
                {
                    var error = new ValidationError("nonFieldErrors", ErrorCode.FileSize, _documentService.LastError.error);
                    return new CommandResult<BestandsDeel>(null, CommandStatus.ValidationError, error);
                }

                bestandsdeel.UploadPartId = result.Context;
                bestandsdeel.Voltooid = true;

                await _context.SaveChangesAsync(cancellationToken);

                // All other finished already?
                if (!versie.BestandsDelen.Where(d => d.Id != bestandsdeel.Id).All(d => d.Voltooid))
                {
                    // No
                    _logger.LogDebug("Uploaded successfully the bestandsdeel {volgnummer}", bestandsdeel.Volgnummer);

                    return new CommandResult<BestandsDeel>(bestandsdeel, CommandStatus.OK);
                }
            }

            // All finished, finalyse by merging all parts and add final document to documentstore
            var document = await MergeBestandsDelenAndAddToDocumentStoreAsync(versie, cancellationToken);

            // Set inhoud to the created document (urn) of the latest version of document
            versie.Inhoud = document.Urn.ToString();
            versie.MultiPartDocumentId = null;

            _context.BestandsDelen.RemoveRange(versie.BestandsDelen);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await LogConflictingValuesAsync(ex);
                throw;
            }

            // Document successfully added to documentstore so we could fire the notification
            await SendNotificationAsync(Actie.update, versie.InformatieObject, cancellationToken);

            _logger.LogDebug(
                "Uploaded successfully the last bestandsdeel and merged all bestansdelen and created document {docuemtnUrn}.",
                bestandsdeel.EnkelvoudigInformatieObjectVersie.Inhoud
            );

            return new CommandResult<BestandsDeel>(bestandsdeel, CommandStatus.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bestandsdeel to DMS [{ProviderPrefix}].", _documentService.ProviderPrefix);
            throw;
        }
    }

    private async Task<Document> MergeBestandsDelenAndAddToDocumentStoreAsync(
        EnkelvoudigInformatieObjectVersie versie,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Completing multi-part document and storing to documentstore...");

        IMultiPartDocument multipartdocument = new MultiPartDocument(versie.MultiPartDocumentId);

        var uploadparts = (from bestandsdeel in versie.BestandsDelen let uploadpart = new UploadPart(bestandsdeel.UploadPartId) select uploadpart)
            .OfType<IUploadPart>()
            .ToList();
        try
        {
            return await _documentService.CompleteMultipartUploadAsync(multipartdocument, uploadparts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not complete multi-part document to final document on DMS [{ProviderPrefix}].",
                _documentService.ProviderPrefix
            );

            await _documentService.AbortMultipartUploadAsync(multipartdocument, cancellationToken);

            throw;
        }
    }
}

class UploadBestandsDeelCommand : IRequest<CommandResult<BestandsDeel>>
{
    public Guid BestandsDeelId { get; internal set; }
    public IFormFile Inhoud { get; internal set; }
    public string Lock { get; internal set; }
}
