using System;
using System.Collections.Generic;
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
using OneGround.ZGW.Documenten.Contracts.v1._1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Extensions;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Zaken.Web.Handlers;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._1;

public class UpdateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<UpdateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<UpdateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObjectVersie>>
{
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
        IDocumentKenmerkenResolver documentKenmerkenResolver
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
        ) { }

    public async Task<CommandResult<EnkelvoudigInformatieObjectVersie>> Handle(
        UpdateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Updating (creating new version of existing) EnkelvoudigInformatieObject....");

        var versie = request.EnkelvoudigInformatieObjectVersie;

        var errors = new List<ValidationError>();

        // Add new version of the EnkelvoudigInformatieObject
        var existingEnkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.AsSplitQuery()
            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
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
        versie.Owner = currentVersie.InformatieObject.Owner;

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            request.ExistingEnkelvoudigInformatieObjectId,
            request.IsPartialUpdate,
            apiVersie: 1.1M,
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
            // Add a new version of the existing EnkelvoudigInformatieObject
            audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(existingEnkelvoudigInformatieObject);

            var informatieObjectType = request.EnkelvoudigInformatieObjectVersie.InformatieObject.InformatieObjectType;

            var indicatieGebruiksrecht = versie.InformatieObject.IndicatieGebruiksrecht;

            versie.BeginRegistratie = DateTime.UtcNow;
            versie.EnkelvoudigInformatieObjectId = request.ExistingEnkelvoudigInformatieObjectId.Value;
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

            versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.InformatieObject);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
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
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; }
    public Guid? ExistingEnkelvoudigInformatieObjectId { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
