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
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Extensions;
using OneGround.ZGW.Documenten.Web.Notificaties;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Documenten.Web.Services.FileValidation;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

public class UpdateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<UpdateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<UpdateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObject2>>
{
    public UpdateEnkelvoudigInformatieObjectCommandHandler(
        ILogger<UpdateEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        DrcDbContext2 context2,
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
        IFileValidationService fileValidationService
    )
        : base(
            logger,
            configuration,
            uriService,
            authorizationContextAccessor,
            documentServicesResolver,
            context,
            context2,
            enkelvoudigInformatieObjectBusinessRuleService,
            nummerGenerator,
            catalogiServiceAgent,
            auditTrailFactory,
            lockGenerator,
            formOptions,
            notificatieService,
            documentKenmerkenResolver,
            fileValidationService
        ) { }

    public async Task<CommandResult<EnkelvoudigInformatieObject2>> Handle(
        UpdateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Updating (creating new version of existing) EnkelvoudigInformatieObject....");

        var versie = request.EnkelvoudigInformatieObjectVersie;

        var errors = new List<ValidationError>();

        ValidateFile(versie, errors);

        // Add new version of the EnkelvoudigInformatieObject
        var existingEnkelvoudigInformatieObjectVersies = await _context2
            .EnkelvoudigInformatieObjecten.AsSplitQuery()
            .Include(e => e.EnkelvoudigInformatieObjectLock.GebruiksRechten)
            .Include(e => e.EnkelvoudigInformatieObjectLock.Verzendingen) // added (missed org)
            .Where(e => e.EnkelvoudigInformatieObjectId == request.ExistingEnkelvoudigInformatieObjectId.Value)
            .ToListAsync(cancellationToken);

        var currentVersie = existingEnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).LastOrDefault();

        if (currentVersie == null)
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(currentVersie, AuthorizationScopes.Documenten.Update))
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.Forbidden);
        }

        versie.Versie = currentVersie.Versie + 1;
        versie.Owner = currentVersie.Owner;
        versie.CatalogusId = currentVersie.CatalogusId;
        versie.InformatieObjectType = currentVersie.InformatieObjectType;
        versie.EnkelvoudigInformatieObjectLockId = currentVersie.EnkelvoudigInformatieObjectLockId;

        // ZZZ
        //await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
        //    versie,
        //    _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
        //    request.ExistingEnkelvoudigInformatieObjectId,
        //    request.IsPartialUpdate,
        //    apiVersie: 1.5M,
        //    errors,
        //    cancellationToken
        //);

        if (errors.Count != 0)
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // Note: Vertrouwelijkheidaanduiding van een informatieobject (drc-007) => get from request or get from Catalogi.InformatieObjectType
        await SetVertrouwelijkheidAanduidingAsync(request.EnkelvoudigInformatieObjectVersie);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            // Add a new version of the existing EnkelvoudigInformatieObject
            audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(currentVersie);

            var informatieObjectType = request.EnkelvoudigInformatieObjectVersie.InformatieObjectType;

            var indicatieGebruiksrecht = versie.IndicatieGebruiksrecht;

            versie.BeginRegistratie = DateTime.UtcNow;
            versie.EnkelvoudigInformatieObjectId = request.ExistingEnkelvoudigInformatieObjectId.Value;
            // Clone the EnkelvoudigInformatieObject from previous version
            //versie.InformatieObject = existingEnkelvoudigInformatieObject;
            versie.InformatieObjectType = informatieObjectType;
            versie.IndicatieGebruiksrecht = indicatieGebruiksrecht;

            // Depending on the specified inhoud and bestandsomvang several ways on how to add documents....
            if (IsDocumentUploadWithBestandsdelen(versie.Bestandsomvang, versie.Inhoud))
            {
                // We have enabled (some) metadata fields for the underlying document provider
                var metadata = new DocumentMeta { Rsin = versie.Owner, Version = versie.Versie };

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
                    return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.ValidationError, errors.ToArray());
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

            await _context2.EnkelvoudigInformatieObjecten.AddAsync(versie, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(versie, versie, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(versie, versie, cancellationToken);
            }

            await _context2.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created new version", versie.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.update, versie, cancellationToken);
        }

        return new CommandResult<EnkelvoudigInformatieObject2>(versie, CommandStatus.OK);
    }
}

public class UpdateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObject2>>
{
    public EnkelvoudigInformatieObject2 EnkelvoudigInformatieObjectVersie { get; internal set; }
    public Guid? ExistingEnkelvoudigInformatieObjectId { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
