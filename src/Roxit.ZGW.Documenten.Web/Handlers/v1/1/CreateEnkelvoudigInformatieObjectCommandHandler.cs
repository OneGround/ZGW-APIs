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

public class CreateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<CreateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<CreateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public CreateEnkelvoudigInformatieObjectCommandHandler(
        ILogger<CreateEnkelvoudigInformatieObjectCommandHandler> logger,
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
        CreateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating EnkelvoudigInformatieObject....");

        var versie = request.EnkelvoudigInformatieObjectVersie;

        if (
            !_authorizationContext.IsAuthorized(
                request.EnkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.InformatieObjectType,
                request.EnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
                AuthorizationScopes.Documenten.Create
            )
        )
        {
            return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.Forbidden);
        }

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        var errors = new List<ValidationError>();

        versie.Versie = 1;

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
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
            // Create the new (initial) version of the EnkelvoudigInformatieObject
            versie.EnkelvoudigInformatieObject.Owner = _rsin;
            versie.BeginRegistratie = DateTime.UtcNow;
            versie.Owner = versie.EnkelvoudigInformatieObject.Owner;

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
                versie.Bestandsomvang = 0;
                versie.Inhoud = null;
            }
            else
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

            // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
            //audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.EnkelvoudigInformatieObject);

            //var enkelvoudigInformatieObjectUrl = _uriService.GetUri(versie.EnkelvoudigInformatieObject);

            //await audittrail.CreatedAsync(versie.EnkelvoudigInformatieObject, versie.EnkelvoudigInformatieObject, cancellationToken);
            // ----

            try
            {
                using var trans = await _context.Database.BeginTransactionAsync(cancellationToken);
                // Saves the new added EnkelvoudigInformationObject and EnkelvoudigInformationObjectVersion
                await _context.SaveChangesAsync(cancellationToken);

                // Sets the 'latest' EnkelvoudigInformationObjectVersion in the parent EnkelvoudigInformatieObject
                versie.EnkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;

                // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
                audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.EnkelvoudigInformatieObject);

                await audittrail.CreatedAsync(versie.EnkelvoudigInformatieObject, versie.EnkelvoudigInformatieObject, cancellationToken);

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

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created", versie.EnkelvoudigInformatieObject.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.create, versie, cancellationToken);
        }

        return new CommandResult<EnkelvoudigInformatieObjectVersie>(versie, CommandStatus.OK);
    }
}

public class CreateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; }
}
