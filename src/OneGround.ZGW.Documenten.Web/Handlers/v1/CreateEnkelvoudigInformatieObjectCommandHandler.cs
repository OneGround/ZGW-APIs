using System;
using System.Collections.Generic;
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
using OneGround.ZGW.Common.Exceptions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.MimeTypes;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Extensions;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Documenten.Web.Services.FileValidation;

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
    private readonly IFileValidationService _fileValidationService;

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
        IFileValidationService fileValidationService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _enkelvoudigInformatieObjectBusinessRuleService = enkelvoudigInformatieObjectBusinessRuleService;
        _nummerGenerator = nummerGenerator;
        _catalogiServiceAgent = catalogiServiceAgent;
        _auditTrailFactory = auditTrailFactory;
        _fileValidationService = fileValidationService;

        _lazyDocumentService = new Lazy<IDocumentService>(() => GetDocumentServiceProvider(documentServicesResolver));
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

        var versie = request.EnkelvoudigInformatieObjectVersie;

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        var errors = new List<ValidationError>();

        ValidateFile(versie, errors);

        // Set version first because business-rule wants to verify against
        EnkelvoudigInformatieObject existingEnkelvoudigInformatieObject = null;
        if (request.ExistingEnkelvoudigInformatieObjectId.HasValue)
        {
            // Add new version of the EnkelvoudigInformatieObject
            existingEnkelvoudigInformatieObject = await _context
                .EnkelvoudigInformatieObjecten.Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                .Include(e => e.EnkelvoudigInformatieObjectVersies)
                .Include(e => e.GebruiksRechten)
                .SingleOrDefaultAsync(e => e.Id == request.ExistingEnkelvoudigInformatieObjectId.Value, cancellationToken);

            if (existingEnkelvoudigInformatieObject == null)
            {
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.NotFound);
            }

            // FUND-1595 latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
            if (existingEnkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersie == null)
            {
                // Not very elegant but it's a temporary work around until we figure out the problem. So IsAuthorized call next will work now
                var latestVersion = existingEnkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last();
                existingEnkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersie = latestVersion;

                _logger.LogWarning("LatestEnkelvoudigInformatieObjectVersie is NULL -> restored");
            }
            // ----

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
                    request.EnkelvoudigInformatieObjectVersie.InformatieObject.InformatieObjectType,
                    request.EnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
                    AuthorizationScopes.Documenten.Create
                )
            )
            {
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Forbidden);
            }

            var informatieobjecttype = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(
                request.EnkelvoudigInformatieObjectVersie.InformatieObject.InformatieObjectType
            );
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

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            request.ExistingEnkelvoudigInformatieObjectId,
            request.IsPartialUpdate,
            apiVersie: 1.0M,
            errors,
            cancellationToken
        );

        if (errors.Count != 0)
        {
            return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // Note: Vertrouwelijkheidaanduiding van een informatieobject (drc-007) => get from request or get from Catalogi.InformatieObjectType
        await SetVertrouwelijkheidAanduidingAsync(request.EnkelvoudigInformatieObjectVersie);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            if (existingEnkelvoudigInformatieObject != null)
            {
                // Add new version of the EnkelvoudigInformatieObject
                audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(existingEnkelvoudigInformatieObject);

                var currentVersie = existingEnkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last();

                var informatieObjectType = request.EnkelvoudigInformatieObjectVersie.InformatieObject.InformatieObjectType;

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
                var organisatie = request.EnkelvoudigInformatieObjectVersie.Bronorganisatie;

                var enkelvoudigInformatieObjectNummer = await _nummerGenerator.GenerateAsync(
                    organisatie,
                    "documenten",
                    id => IsEnkelvoudigInformatieObjectVersieUnique(organisatie, id, versie.Versie),
                    cancellationToken
                );

                versie.Identificatie = enkelvoudigInformatieObjectNummer;
            }

            await _context.EnkelvoudigInformatieObjectVersies.AddAsync(versie, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            try
            {
                using var trans = await _context.Database.BeginTransactionAsync(cancellationToken);
                // Saves the new added EnkelvoudigInformationObject and EnkelvoudigInformationObjectVersion
                await _context.SaveChangesAsync(cancellationToken);

                // Sets the 'latest' EnkelvoudigInformationObjectVersion in the parent EnkelvoudigInformatieObject
                versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

                audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.InformatieObject);

                if (request.ExistingEnkelvoudigInformatieObjectId.HasValue)
                {
                    if (request.IsPartialUpdate)
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

                await trans.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await LogConflictingValuesAsync(ex);
                throw;
            }
            catch (DbUpdateException ex)
            {
                LogFunctionalEntityKeys(ex.Message, versie);
                throw;
            }
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

    private void LogFunctionalEntityKeys(string message, EnkelvoudigInformatieObjectVersie versie)
    {
        _logger.LogWarning("Database exception occured: {message}. Key dump creating/updating EnkelvoudigInformatieObject version...", message);
        _logger.LogWarning(
            "-{labelVersieEnkelvoudigInformatieObjectId}: {versieEnkelvoudigInformatieObjectId}",
            nameof(versie.EnkelvoudigInformatieObjectId),
            versie.EnkelvoudigInformatieObjectId
        );
        _logger.LogWarning("-{labelVersieOwner}: {versieOwner}", nameof(versie.Owner), versie.Owner);
        _logger.LogWarning("-{labelVersieBronorganisatie}: {versieBronorganisatie}", nameof(versie.Bronorganisatie), versie.Bronorganisatie);
        _logger.LogWarning("-{labelVersieIdentificatie}: {versieIdentificatie}", nameof(versie.Identificatie), versie.Identificatie);
        _logger.LogWarning("-{labelVersieVersie}: {versieVersie}", nameof(versie.Versie), versie.Versie);
    }

    private void ValidateFile(EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie, List<ValidationError> errors)
    {
        if (string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Inhoud))
            return;

        try
        {
            _fileValidationService.Validate(enkelvoudigInformatieObjectVersie.Bestandsnaam);
        }
        catch (OneGroundException)
        {
            var error = new ValidationError(
                "bestandsnaam",
                ErrorCode.Invalid,
                "Het document is geweigerd omdat het type van het bestand niet is toegestaan."
            );

            errors.Add(error);
        }
    }

    private IDocumentService DocumentService => _lazyDocumentService.Value;

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

class CreateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; }
    public Guid? ExistingEnkelvoudigInformatieObjectId { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
