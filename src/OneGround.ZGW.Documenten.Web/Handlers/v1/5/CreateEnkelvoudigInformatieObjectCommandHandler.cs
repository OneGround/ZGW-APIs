using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
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

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

public class CreateEnkelvoudigInformatieObjectCommandHandler
    : MutatieEnkelvoudigInformatieObjectCommandHandler<CreateEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<CreateEnkelvoudigInformatieObjectCommand, CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    // TODO: MIGRATOR AND EXPORTER TESTING.....
    private readonly IAuditTrailMigrator _auditTrailMigrator;
    private readonly IAuditTrailExporter _auditTrailExporter;

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
        INotificatieService notificatieService,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        // TODO: MIGRATOR AND EXPORTER TESTING.....
        IAuditTrailMigrator auditTrailMigrator,
        IAuditTrailExporter auditTrailExporter
    // ----
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
        // TODO: MIGRATOR AND EXPORTER TESTINGG.....
        _auditTrailMigrator = auditTrailMigrator;
        _auditTrailExporter = auditTrailExporter;
        // ----
    }

    public async Task<CommandResult<EnkelvoudigInformatieObjectVersie>> Handle(
        CreateEnkelvoudigInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating EnkelvoudigInformatieObject....");

        /*
        // TODO: MIGRATOR AND EXPORTER TESTING.....
        //
        // Note: Test AuditTrailMigrator

        var hoofdobjectId = new Guid("f767ccce-43a0-4ff4-84b8-13b7b502af63");

        await _auditTrailMigrator.MigrateAsync(hoofdobjectId, cancellationToken);

        //
        // Note: Test AuditTrailMigrator

        await _auditTrailExporter.ExportAsync(hoofdobjectId, legacy: true, cancellationToken);

        await Task.Delay(1100);

        await _auditTrailExporter.ExportAsync(hoofdobjectId, legacy: false, cancellationToken);

        return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.NotFound);
        // ----
        */

        var versie = request.EnkelvoudigInformatieObjectVersie;

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

        versie.SetLinkToNullWhenInvalid();
        versie.EscapeBestandsNaamWhenInvalid();

        var errors = new List<ValidationError>();

        // Create the new (initial) version of the EnkelvoudigInformatieObject
        versie.Versie = 1;
        versie.InformatieObject.Owner = _rsin;
        versie.BeginRegistratie = DateTime.UtcNow;
        versie.Owner = _rsin;

        await _enkelvoudigInformatieObjectBusinessRuleService.ValidateAsync(
            versie,
            _applicationConfiguration.IgnoreInformatieObjectTypeValidation,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.5M,
            errors,
            cancellationToken
        );

        if (errors.Count != 0)
        {
            return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, errors.ToArray());
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

        // Note: Vertrouwelijkheidaanduiding van een informatieobject (drc-007) => get from request or get from Catalogi.InformatieObjectType
        await SetVertrouwelijkheidAanduidingAsync(request.EnkelvoudigInformatieObjectVersie);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, legacy: false))
        {
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
                var owner = request.EnkelvoudigInformatieObjectVersie.Owner;

                var enkelvoudigInformatieObjectNummer = await _nummerGenerator.GenerateAsync(
                    owner,
                    "documenten",
                    id => IsEnkelvoudigInformatieObjectVersieUnique(owner, id),
                    cancellationToken
                );

                versie.Identificatie = enkelvoudigInformatieObjectNummer;
            }

            await _context.EnkelvoudigInformatieObjectVersies.AddAsync(versie, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            using var trans = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Saves the new added EnkelvoudigInformationObject and EnkelvoudigInformationObjectVersion.
            // Handle potential race condition on INSERT with unique constraint in Postgres:
            // if two requests hit this API simultaneously, a "check-then-act" pattern can fail and the INSERT may violate the unique constraint.
            try
            {
                // Try to save changes with potential concurrency conflict
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // Handle preventing race-condition where another process has created already EnkelvoudigInformatieObject with the same identificatie+owner+versie after our initial read but before our insert
                await RollbackDocumentJustAddedToDmsAsync(versie.Inhoud);

                var error = new ValidationError("identificatie", ErrorCode.Unique, "Deze identificatie bestaat al voor deze organisatie.");
                return new CommandResult<EnkelvoudigInformatieObjectVersie>(null, CommandStatus.ValidationError, error);
            }
            catch (Exception)
            {
                // Rollback and re-throw the exception to be handled by the global exception handler, but first try to rollback the document just added to DMS if any
                await RollbackDocumentJustAddedToDmsAsync(versie.Inhoud);
                throw;
            }

            // Sets the 'latest' EnkelvoudigInformationObjectVersion in the parent EnkelvoudigInformatieObject
            versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId = versie.Id;
            versie.InformatieObject.LatestEnkelvoudigInformatieObjectVersie = versie;

            versie.LatestInformatieObject = versie.InformatieObject;

            versie.InformatieObject.CatalogusId = catalogusId;

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(versie.InformatieObject);

            await audittrail.CreatedAsync(versie.InformatieObject, versie.InformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await trans.CommitAsync(cancellationToken);
        }

        _logger.LogDebug("EnkelvoudigInformatieObject {Id} successfully created", versie.InformatieObject.Id);

        // Note: Do not send notification using bestandsdelen (this is done when all bestandsdelen are uploaded an checkin is called)
        if (versie.BestandsDelen.Count == 0)
        {
            await SendNotificationAsync(Actie.create, versie.InformatieObject, cancellationToken); // TODO: Check older versions how this reacts!!!!!
        }

        return new CommandResult<EnkelvoudigInformatieObjectVersie>(versie, CommandStatus.OK);
    }

    private async Task RollbackDocumentJustAddedToDmsAsync(string urnDocument)
    {
        if (string.IsNullOrEmpty(urnDocument))
        {
            return;
        }

        try
        {
            await DocumentService.DeleteDocumentAsync(new DocumentUrn(urnDocument));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback document with urn {UrnDocument} just added to DMS", urnDocument);
            // Do not throw an exception on the rollback failure
        }
    }
}

public class CreateEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<EnkelvoudigInformatieObjectVersie>>
{
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; internal set; }
}
