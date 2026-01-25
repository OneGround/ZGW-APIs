using System;
using System.Collections.Generic;
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

public class CreateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<CreateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<CreateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObject2>>
{
    public CreateEnkelvoudigInformatieObjectCommandHandler(
        ILogger<CreateEnkelvoudigInformatieObjectCommandHandler> logger,
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
        CreateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating EnkelvoudigInformatieObject....");

        var versie = request.EnkelvoudigInformatieObjectVersie;

        if (
            !_authorizationContext.IsAuthorized(
                request.EnkelvoudigInformatieObjectVersie.InformatieObjectType,
                request.EnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
                AuthorizationScopes.Documenten.Create
            )
        )
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.Forbidden);
        }

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        var errors = new List<ValidationError>();

        ValidateFile(versie, errors);

        versie.Versie = 1;

        // ZZZ
        //await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
        //    versie,
        //    _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
        //    existingEnkelvoudigInformatieObjectId: null,
        //    isPartialUpdate: false,
        //    apiVersie: 1.5M,
        //    errors,
        //    cancellationToken
        //);

        if (errors.Count != 0)
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var informatieobjecttype = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(
            request.EnkelvoudigInformatieObjectVersie.InformatieObjectType
        );
        if (!informatieobjecttype.Success)
        {
            return new CommandResult<EnkelvoudigInformatieObject2>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("enkelvoudiginformatieobjecttype", informatieobjecttype.Error.Code, informatieobjecttype.Error.Title)
            );
        }
        var catalogusId = _uriService.GetId(informatieobjecttype.Response.Catalogus);

        // Note: Vertrouwelijkheidaanduiding van een informatieobject (drc-007) => get from request or get from Catalogi.InformatieObjectType
        await SetVertrouwelijkheidAanduidingAsync(request.EnkelvoudigInformatieObjectVersie);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            // Create the new (initial) version of the EnkelvoudigInformatieObject
            versie.Owner = _rsin;
            versie.BeginRegistratie = DateTime.UtcNow;
            versie.Owner = versie.Owner;
            versie.CatalogusId = catalogusId;

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
                versie.Bestandsomvang = 0;
                versie.Inhoud = null;
            }
            else
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
                    id => IsEnkelvoudigInformatieObjectVersieUnique2(organisatie, id, versie.Versie),
                    cancellationToken
                );

                versie.Identificatie = enkelvoudigInformatieObjectNummer;
            }

            var @lock = new EnkelvoudigInformatieObjectLock2 { Id = Guid.NewGuid(), Owner = _rsin };

            versie.EnkelvoudigInformatieObjectLockId = @lock.Id;

            await _context2.EnkelvoudigInformatieObjectLocks.AddAsync(@lock, cancellationToken);
            await _context2.EnkelvoudigInformatieObjecten.AddAsync(versie, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie);

            await audittrail.CreatedAsync(versie, versie, cancellationToken);

            try
            {
                await _context2.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) { }
        }

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created", versie.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.create, versie, cancellationToken); // TODO: Check older versions how this reacts!!!!!
        }

        return new CommandResult<EnkelvoudigInformatieObject2>(versie, CommandStatus.OK);
    }
}

public class CreateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObject2>>
{
    public EnkelvoudigInformatieObject2 EnkelvoudigInformatieObjectVersie { get; internal set; }
}
