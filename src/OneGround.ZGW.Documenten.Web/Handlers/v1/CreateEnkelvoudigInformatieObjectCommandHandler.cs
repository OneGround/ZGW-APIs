using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.MimeTypes;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Extensions;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class CreateEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<CreateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<CreateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    private readonly DrcDbContext _context;
    private readonly IEnkelvoudigInformatieObjectBusinessRuleService _enkelvoudigInformatieObjectBusinessRuleService;
    private readonly INummerGenerator _nummerGenerator;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly Lazy<IDocumentService> _lazyDocumentService;
    private readonly IEnkelvoudigInformatieObjectMerger _entityMerger;

    public CreateEnkelvoudigInformatieObjectCommandHandler(
        ILogger<CreateEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        INummerGenerator nummerGenerator,
        IDocumentServicesResolver documentServicesResolver,
        IEnkelvoudigInformatieObjectBusinessRuleService enkelvoudigInformatieObjectBusinessRuleService,
        INotificatieService notificatieService,
        ICatalogiServiceAgent catalogiServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IEnkelvoudigInformatieObjectMergerFactory entityMergerFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _enkelvoudigInformatieObjectBusinessRuleService = enkelvoudigInformatieObjectBusinessRuleService;
        _nummerGenerator = nummerGenerator;
        _catalogiServiceAgent = catalogiServiceAgent;
        _auditTrailFactory = auditTrailFactory;

        _lazyDocumentService = new Lazy<IDocumentService>(() => GetDocumentServiceProvider(documentServicesResolver));

        _entityMerger = entityMergerFactory.Create<EnkelvoudigInformatieObjectUpdateRequestDto>();
    }

    private IDocumentService GetDocumentServiceProvider(IDocumentServicesResolver documentServicesResolver)
    {
        var documentService =
            documentServicesResolver.GetDefault() ?? throw new InvalidOperationException("Could not resolve default documentservice provider.");
        _logger.LogDebug("Default DMS provider '{ProviderPrefix}' [{documentService}]", documentService.ProviderPrefix, documentService);

        return documentService;
    }

    public async Task<CommandResult<EnkelvoudigInformatieObjectVersie>> Handle(
        CreateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating EnkelvoudigInformatieObject....");

        var errors = new List<ValidationError>();

        var versie = request.EnkelvoudigInformatieObjectVersie;

        // Use ReadCommitted isolation level:
        // - FOR UPDATE provides pessimistic row-level locking (prevents concurrent modifications)
        // - xmin (configured in EnkelvoudigInformatieObject) provides optimistic concurrency detection (detects any changes since read)
        // - ReadCommitted allows better concurrency than Serializable for this use case
        // - The combination prevents both lost updates (via FOR UPDATE) and write skew (via xmin)
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        bool isPartialUpdate = request.PartialObject != null && request.EnkelvoudigInformatieObjectVersie == null;

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        // Set version first because business-rule wants to verify against
        EnkelvoudigInformatieObject existingEnkelvoudigInformatieObject = null;
        if (request.ExistingEnkelvoudigInformatieObjectId.HasValue)
        {
            // Add new version of the EnkelvoudigInformatieObject
            existingEnkelvoudigInformatieObject = await _context
                .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [request.ExistingEnkelvoudigInformatieObjectId.Value])
                .Where(rsinFilter)
                .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                .Include(e => e.EnkelvoudigInformatieObjectVersies)
                .Include(e => e.GebruiksRechten)
                .SingleOrDefaultAsync(e => e.Id == request.ExistingEnkelvoudigInformatieObjectId.Value, cancellationToken);

            if (existingEnkelvoudigInformatieObject == null)
            {
                // The object might be locked OR not exist - check if it exists without lock
                var exists = await _context
                    .EnkelvoudigInformatieObjecten.Where(rsinFilter)
                    .AnyAsync(e => e.Id == request.ExistingEnkelvoudigInformatieObjectId, cancellationToken);

                if (!exists)
                {
                    // Object truly doesn't exist
                    return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.NotFound);
                }

                // Object exists but is locked by another process
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.Conflict,
                    $"Het enkelvoudiginformatieobject {request.ExistingEnkelvoudigInformatieObjectId} is vergrendeld door een andere bewerking."
                );

                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Conflict, error);
            }

            if (isPartialUpdate)
            {
                // Partial update (e.g. for PATCH endpoint) so merge the partial object provided by the client with the existing entity
                versie = _entityMerger.TryMergeWithPartial(request.PartialObject, existingEnkelvoudigInformatieObject, errors);
                if (errors.Count != 0)
                {
                    return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }
            else
            {
                // Full update (e.g. for PUT endpoint) with EnkelvoudigInformatieObjectVersie provided by the client
                versie = request.EnkelvoudigInformatieObjectVersie;
            }

            if (!_authorizationContext.IsAuthorized(existingEnkelvoudigInformatieObject, AuthorizationScopes.Documenten.Update))
            {
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Forbidden);
            }

            var currentVersie = existingEnkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last();

            versie.Versie = currentVersie.Versie + 1;
        }
        else
        {
            if (
                !_authorizationContext.IsAuthorized(
                    versie.InformatieObject.InformatieObjectType,
                    versie.Vertrouwelijkheidaanduiding,
                    AuthorizationScopes.Documenten.Create
                )
            )
            {
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Forbidden);
            }

            var informatieobjecttype = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(versie.InformatieObject.InformatieObjectType);
            if (!informatieobjecttype.Success)
            {
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(
                    null,
                    CommandStatus.ValidationError,
                    new ValidationError("enkelvoudiginformatieobjecttype", informatieobjecttype.Error.Code, informatieobjecttype.Error.Title)
                );
            }
            var catalogusId = _uriService.GetId(informatieobjecttype.Response.Catalogus);

            versie.Versie = 1;
            versie.InformatieObject.CatalogusId = catalogusId;
        }

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            request.ExistingEnkelvoudigInformatieObjectId,
            isPartialUpdate,
            apiVersie: 1.0M,
            errors,
            cancellationToken
        );

        if (errors.Count != 0)
        {
            return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // Note: Vertrouwelijkheidaanduiding van een informatieobject (drc-007) => get from request or get from Catalogi.InformatieObjectType
        await SetVertrouwelijkheidAanduidingAsync(versie);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            if (existingEnkelvoudigInformatieObject != null)
            {
                // Add new version of the EnkelvoudigInformatieObject
                audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(existingEnkelvoudigInformatieObject);

                var currentVersie = existingEnkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last();

                var informatieObjectType = versie.InformatieObject.InformatieObjectType;

                var indicatieGebruiksrecht = versie.InformatieObject.IndicatieGebruiksrecht;

                versie.Bestandsomvang = currentVersie.Bestandsomvang;
                versie.BeginRegistratie = DateTime.UtcNow;
                versie.EnkelvoudigInformatieObjectId = request.ExistingEnkelvoudigInformatieObjectId.Value;
                // Clone the EnkelvoudigInformatieObject from previous version
                versie.InformatieObject = existingEnkelvoudigInformatieObject;
                versie.InformatieObject.InformatieObjectType = informatieObjectType;
                versie.InformatieObject.IndicatieGebruiksrecht = indicatieGebruiksrecht;

                // New base64 encoded Inhoud specified (in request) or existing document-urn from merge operation?
                if (!versie.Inhoud.IsAnyDocumentUrn())
                {
                    // Yes; keep create new version and add it to documentstore
                    await AddDocumentToDocumentStore(versie, cancellationToken);
                }
            }
            else
            {
                versie.InformatieObject.Owner = _rsin;
                versie.BeginRegistratie = DateTime.UtcNow;

                // Create new (initial) version of the EnkelvoudigInformatieObject
                await AddDocumentToDocumentStore(versie, cancellationToken);
            }
            versie.Owner = versie.InformatieObject.Owner;

            // Use external identificatie if specified generate otherwise
            if (string.IsNullOrEmpty(versie.Identificatie))
            {
                var organisatie = versie.Bronorganisatie;

                var enkelvoudigInformatieObjectNummer = await _nummerGenerator.GenerateAsync(
                    organisatie,
                    "documenten",
                    id => IsEnkelvoudigInformatieObjectVersieUnique(organisatie, id, versie.Versie),
                    cancellationToken
                );

                versie.Identificatie = enkelvoudigInformatieObjectNummer;
            }

            await _context.EnkelvoudigInformatieObjectVersies.AddAsync(versie, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            // Saves the new added EnkelvoudigInformationObject and EnkelvoudigInformationObjectVersion
            await _context.SaveChangesAsync(cancellationToken);

            // Sets the 'latest' EnkelvoudigInformationObjectVersion in the parent EnkelvoudigInformatieObject
            versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.InformatieObject);

            if (request.ExistingEnkelvoudigInformatieObjectId.HasValue)
            {
                if (isPartialUpdate)
                {
                    await audittrail.PatchedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
                }
                else
                {
                    await audittrail.UpdatedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
                }
            }
            else
            {
                await audittrail.CreatedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created", versie.InformatieObject.Id);

        var actie = request.ExistingEnkelvoudigInformatieObjectId.HasValue ? Actie.update : Actie.create;

        await SendNotificationAsync(actie, versie.InformatieObject, cancellationToken);

        return new CommandResult<EnkelvoudigInformatieObjectVersie>(versie, CommandStatus.OK);
    }

    private bool IsEnkelvoudigInformatieObjectVersieUnique(string organisatie, string identificatie, int versie)
    {
        return !_context
            .EnkelvoudigInformatieObjectVersies.AsNoTracking()
            .Any(e => e.Identificatie == identificatie && e.Bronorganisatie == organisatie && e.Versie == versie);
    }

    private async Task AddDocumentToDocumentStore(
        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie,
        CancellationToken cancellationToken
    )
    {
        var contentType = string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Formaat)
            ? MimeTypeHelper.GetMimeType(enkelvoudigInformatieObjectVersie.Bestandsnaam)
            : enkelvoudigInformatieObjectVersie.Formaat;

        // We have enabled (some) metadata fields for the underlying document provider
        var metadata = new DocumentMeta
        {
            Rsin = enkelvoudigInformatieObjectVersie.InformatieObject.Owner,
            Version = enkelvoudigInformatieObjectVersie.Versie,
        };

        try
        {
            var document = await DocumentService.AddDocumentAsync(
                enkelvoudigInformatieObjectVersie.Inhoud,
                enkelvoudigInformatieObjectVersie.Bestandsnaam ?? "",
                contentType,
                metadata,
                cancellationToken: cancellationToken
            );

            enkelvoudigInformatieObjectVersie.Inhoud = $"{document.Urn}";
            enkelvoudigInformatieObjectVersie.Bestandsomvang = document.Size;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding (a new version of) the document to DMS [{ProviderPrefix}].", DocumentService.ProviderPrefix);
            throw;
        }
    }

    private async Task SetVertrouwelijkheidAanduidingAsync(EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie)
    {
        if (!enkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding.HasValue)
        {
            // This value is guaranteed to be read from the cache (when validation is enabled which is normally the case of course!)
            var informatieObjectType = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(
                enkelvoudigInformatieObjectVersie.InformatieObject.InformatieObjectType
            );

            if (
                Enum.TryParse<VertrouwelijkheidAanduiding>(
                    informatieObjectType.Response.VertrouwelijkheidAanduiding,
                    out var vertrouwelijkheidaanduiding
                )
            )
            {
                enkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding = vertrouwelijkheidaanduiding;
            }
        }
    }

    private IDocumentService DocumentService => _lazyDocumentService.Value;

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

class CreateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; }
    public Guid? ExistingEnkelvoudigInformatieObjectId { get; internal set; } // For PUT endpoint, contains the full update sent by the client, For POST endpoint this is null
    public dynamic PartialObject { get; internal set; } // For PATCH endpoint, contains the partial update sent by the client
}
