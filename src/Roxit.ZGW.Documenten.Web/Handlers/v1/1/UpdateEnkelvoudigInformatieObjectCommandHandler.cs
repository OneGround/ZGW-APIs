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
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1._1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Services;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1;
using Roxit.ZGW.Documenten.Web.Extensions;
using Roxit.ZGW.Documenten.Web.Notificaties;
using Roxit.ZGW.Documenten.Web.Services;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1._1;

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
        INotificatieService notificatieService
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
            notificatieService
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
        versie.Owner = currentVersie.EnkelvoudigInformatieObject.Owner;

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

            var informatieObjectType = request.EnkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.InformatieObjectType;

            var indicatieGebruiksrecht = versie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht;

            versie.BeginRegistratie = DateTime.UtcNow;
            versie.EnkelvoudigInformatieObjectId = request.ExistingEnkelvoudigInformatieObjectId.Value;
            // Clone the EnkelvoudigInformatieObject from previous version
            versie.EnkelvoudigInformatieObject = existingEnkelvoudigInformatieObject;
            versie.EnkelvoudigInformatieObject.InformatieObjectType = informatieObjectType;
            versie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht = indicatieGebruiksrecht;

            // Depending on the specified inhoud and bestandsomvang several ways on how to add documents....
            if (IsDocumentUploadWithBestandsdelen(versie.Bestandsomvang, versie.Inhoud))
            {
                // We have enabled (some) metadata fields for the underlying document provider
                var metadata = new DocumentMeta { Rsin = versie.EnkelvoudigInformatieObject.Owner, Version = versie.Versie };

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

            versie.EnkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.EnkelvoudigInformatieObject);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(versie.EnkelvoudigInformatieObject, versie.EnkelvoudigInformatieObject, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(versie.EnkelvoudigInformatieObject, versie.EnkelvoudigInformatieObject, cancellationToken);
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

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created new version", versie.EnkelvoudigInformatieObject.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.update, versie, cancellationToken);
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
