using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.MimeTypes;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Documenten.Web.Services.FileValidation;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

public abstract class MutatieEnkelvoudigInformatieObjectCommandHandler<T> : DocumentenBaseHandler<T>
{
    protected readonly IEnkelvoudigInformatieObjectBusinessRuleService _enkelvoudigInformatieObjectBusinessRuleService;
    protected readonly INummerGenerator _nummerGenerator;
    protected readonly IAuditTrailFactory _auditTrailFactory;
    protected readonly DrcDbContext _context;
    protected readonly ICatalogiServiceAgent _catalogiServiceAgent;
    protected readonly Lazy<IDocumentService> _lazyDocumentService;
    protected readonly ILockGenerator _lockGenerator;
    protected readonly IOptions<FormOptions> _formOptions;
    private readonly IFileValidationService _fileValidationService;

    protected MutatieEnkelvoudigInformatieObjectCommandHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentServicesResolver documentServicesResolver,
        DrcDbContext context,
        IEnkelvoudigInformatieObjectBusinessRuleService enkelvoudigInformatieObjectBusinessRuleService,
        INummerGenerator nummerGenerator,
        ICatalogiServiceAgent catalogiServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        ILockGenerator lockGenerator,
        IOptions<FormOptions> formOptions,
        INotificatieService notificatieService,
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
        _lockGenerator = lockGenerator;
        _formOptions = formOptions;
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

    protected bool IsEnkelvoudigInformatieObjectVersieUnique(string organisatie, string identificatie, int versie)
    {
        return !_context
            .EnkelvoudigInformatieObjectVersies.AsNoTracking()
            .Any(e => e.Identificatie == identificatie && e.Bronorganisatie == organisatie && e.Versie == versie);
    }

    protected void ValidateFile(
        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Inhoud))
            return;

        try
        {
            _fileValidationService.Validate(enkelvoudigInformatieObjectVersie.Bestandsnaam, cancellationToken);
        }
        catch (Exception)
        {
            var error = new ValidationError(
                "inhoud",
                ErrorCode.Invalid,
                "Het document is geweigerd omdat het type van het bestand niet is toegestaan.");
            errors.Add(error);
        }
    }

    protected async Task<(string inhoud, long bestandsomvang)> TryAddDocumentToDocumentStore(
        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Inhoud) && enkelvoudigInformatieObjectVersie.Bestandsomvang > 0)
        {
            errors.Add(new ValidationError("inhoud", ErrorCode.Required, ""));
            return (inhoud: default, bestandsomvang: default);
        }

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

            return (inhoud: $"{document.Urn}", bestandsomvang: document.Size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding (a new version of) the document to DMS [{ProviderPrefix}].", DocumentService.ProviderPrefix);
            throw;
        }
    }

    protected void AddBestandsDelenToEnkelvoudigeInformatieObjectVersie(EnkelvoudigInformatieObjectVersie versie)
    {
        _logger.LogDebug("Adding bestandsdelen in database....");

        long maxPerBestandsdeel = _formOptions.Value.MultipartBodyLengthLimit;

        int numBestanden = (int)(versie.Bestandsomvang / maxPerBestandsdeel);
        if (versie.Bestandsomvang % maxPerBestandsdeel != 0)
            numBestanden++;

        long restBestandsomvang = versie.Bestandsomvang;

        for (int volgnummer = 1; volgnummer <= numBestanden; volgnummer++)
        {
            var bestandsdeel = new BestandsDeel
            {
                Omvang = (int)(restBestandsomvang < maxPerBestandsdeel ? restBestandsomvang : maxPerBestandsdeel),
                Volgnummer = volgnummer,
                Voltooid = false,
                EnkelvoudigInformatieObjectVersieId = versie.Id,
                EnkelvoudigInformatieObjectVersie = versie,
            };

            _context.BestandsDelen.Add(bestandsdeel);

            restBestandsomvang -= maxPerBestandsdeel;
        }

        if (!versie.InformatieObject.Locked)
        {
            versie.InformatieObject.Locked = true;
            versie.InformatieObject.Lock = _lockGenerator.Generate();
        }

        _logger.LogDebug("{numBestanden} bestandsdelen added in database", numBestanden);
    }

    protected async Task SetVertrouwelijkheidAanduidingAsync(EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie)
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

    protected bool IsDocumentUploadWithBestandsdelen(long bestandsomvang, string inhoud)
    {
        return bestandsomvang > 0 && string.IsNullOrEmpty(inhoud);
    }

    protected bool IsDocumentMetaOnly(long bestandsomvang, string inhoud)
    {
        return bestandsomvang == 0 && string.IsNullOrEmpty(inhoud);
    }

    protected IDocumentService DocumentService => _lazyDocumentService.Value;

    protected AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };

    protected void LogFunctionalEntityKeys(string message, EnkelvoudigInformatieObjectVersie versie)
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
}
