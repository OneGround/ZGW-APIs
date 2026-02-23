using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Extensions;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

public class UpdateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<UpdateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<UpdateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    private readonly IEnkelvoudigInformatieObjectMerger _entityMerger;

    public UpdateEnkelvoudigInformatieObjectCommandHandler(
        ILogger<UpdateEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        INummerGenerator nummerGenerator,
        IDocumentServicesResolver documentServicesResolver,
        IEnkelvoudigInformatieObjectBusinessRuleService enkelvoudigInformatieObjectBusinessRuleService,
        ICatalogiServiceAgent catalogiServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ILockGenerator lockGenerator,
        IOptions<FormOptions> formOptions,
        INotificatieService notificatieService,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IEnkelvoudigInformatieObjectMergerFactory entityMergerFactory
    )
        : base(
            logger,
            configuration,
            uriService,
            authorizationContextAccessor,
            documentServicesResolver,
            context,
            enkelvoudigInformatieObjectBusinessRuleService,
            nummerGenerator,
            catalogiServiceAgent,
            auditTrailFactory,
            lockGenerator,
            formOptions,
            notificatieService,
            documentKenmerkenResolver
        )
    {
        _entityMerger = entityMergerFactory.Create<EnkelvoudigInformatieObjectUpdateRequestDto>();
    }

    public async Task<CommandResult<EnkelvoudigInformatieObjectVersie>> Handle(
        UpdateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Updating (creating new version of existing) EnkelvoudigInformatieObject....");

        var errors = new List<ValidationError>();

        // Use ReadCommitted isolation level:
        // - FOR UPDATE provides pessimistic row-level locking (prevents concurrent modifications)
        // - xmin (configured in EnkelvoudigInformatieObject) provides optimistic concurrency detection (detects any changes since read)
        // - ReadCommitted allows better concurrency than Serializable for this use case
        // - The combination prevents both lost updates (via FOR UPDATE) and write skew (via xmin)
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        // First, try to acquire lock on the EnkelvoudigInformatieObject
        var existingEnkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [request.ExistingEnkelvoudigInformatieObjectId])
            .Where(rsinFilter)
            .AsSplitQuery()
            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .SingleOrDefaultAsync(e => e.Id == request.ExistingEnkelvoudigInformatieObjectId, cancellationToken);

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

        bool isPartialUpdate = request.PartialObject != null && request.EnkelvoudigInformatieObjectVersie == null;

        EnkelvoudigInformatieObjectVersie versie;
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

        var currentVersie = existingEnkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersie;

        versie.Versie = currentVersie.Versie + 1;
        versie.Owner = currentVersie.InformatieObject.Owner;

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            request.ExistingEnkelvoudigInformatieObjectId,
            isPartialUpdate,
            apiVersie: 1.5M,
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
            // Add a new version of the existing EnkelvoudigInformatieObject
            audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(existingEnkelvoudigInformatieObject);

            var informatieObjectType = versie.InformatieObject.InformatieObjectType;

            var indicatieGebruiksrecht = versie.InformatieObject.IndicatieGebruiksrecht;

            versie.BeginRegistratie = DateTime.UtcNow;
            versie.EnkelvoudigInformatieObjectId = request.ExistingEnkelvoudigInformatieObjectId;
            // Clone the EnkelvoudigInformatieObject from previous version
            versie.InformatieObject = existingEnkelvoudigInformatieObject;
            versie.InformatieObject.InformatieObjectType = informatieObjectType;
            versie.InformatieObject.IndicatieGebruiksrecht = indicatieGebruiksrecht;

            // Depending on the specified inhoud and bestandsomvang several ways on how to add documents....
            if (IsDocumentUploadWithBestandsdelen(versie.Bestandsomvang, versie.Inhoud))
            {
                // We have enabled (some) metadata fields for the underlying document provider
                var metadata = new DocumentMeta { Rsin = versie.InformatieObject.Owner, Version = versie.Versie };

                var result = await DocumentService.InitiateMultipartUploadAsync(versie.Bestandsnaam, metadata, cancellationToken);

                versie.MultiPartDocumentId = result.Context;

                AddBestandsDelenToEnkelvoudigeInformatieObjectVersie(versie);

                versie.Inhoud = null;
            }
            else if (IsDocumentMetaOnly(versie.Bestandsomvang, versie.Inhoud))
            {
                versie.Inhoud = null;
            }
            else if (!versie.Inhoud.IsAnyDocumentUrn()) // New base64 encoded Inhoud specified (in request) or existing document-urn from merge operation?
            {
                var (inhoud, bestandsomvang) = await TryAddDocumentToDocumentStore(versie, errors, cancellationToken);
                if (errors.Count != 0)
                {
                    return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, errors.ToArray());
                }
                versie.Inhoud = inhoud;
                versie.Bestandsomvang = bestandsomvang;
            }

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

            versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.InformatieObject);

            if (isPartialUpdate)
            {
                await audittrail.PatchedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created new version", versie.InformatieObject.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.update, versie.InformatieObject, cancellationToken);
        }

        return new CommandResult<EnkelvoudigInformatieObjectVersie>(versie, CommandStatus.OK);
    }
}

public class UpdateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public Guid ExistingEnkelvoudigInformatieObjectId { get; internal set; }
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; } // For PUT endpoint, contains the full update sent by the client
    public dynamic PartialObject { get; internal set; } // For PATCH endpoint, contains the partial update sent by the client
}
