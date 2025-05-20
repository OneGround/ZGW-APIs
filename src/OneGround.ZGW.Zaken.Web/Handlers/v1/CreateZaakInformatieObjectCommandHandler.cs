using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.Messaging.Contracts;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakInformatieObjectCommandHandler
    : ZakenBaseHandler<CreateZaakInformatieObjectCommandHandler>,
        IRequestHandler<CreateZaakInformatieObjectCommand, CommandResult<ZaakInformatieObject>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakInformatieObjectBusinessRuleService _zaakInformatieObjectBusinessRuleService;
    private readonly IRequestClient<IAddObjectInformatieObject> _client;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly ICachedDocumentenServiceAgent _documentenServiceAgent;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public CreateZaakInformatieObjectCommandHandler(
        ILogger<CreateZaakInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IZaakInformatieObjectBusinessRuleService zaakInformatieObjectBusinessRuleService,
        INotificatieService notificatieService,
        IRequestClient<IAddObjectInformatieObject> client,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        ICatalogiServiceAgent catalogiServiceAgent,
        ICachedDocumentenServiceAgent documentenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        IBatchIdAccessor batchIdAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _zaakInformatieObjectBusinessRuleService = zaakInformatieObjectBusinessRuleService;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _client = client;
        _catalogiServiceAgent = catalogiServiceAgent;
        _documentenServiceAgent = documentenServiceAgent;
        _correlationContextAccessor = correlationContextAccessor;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task<CommandResult<ZaakInformatieObject>> Handle(CreateZaakInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakInformatieObject and validating....");

        var zaakInformatieObject = request.ZaakInformatieObject;

        var errors = new List<ValidationError>();

        await _zaakInformatieObjectBusinessRuleService.ValidateAsync(
            zaakInformatieObject,
            request.ZaakUrl,
            _applicationConfiguration.IgnoreInformatieObjectValidation,
            errors
        );

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            errors.Add(new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend."));
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.NotFound, errors.ToArray());
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        var informatieObject = await GetInformatieObjectAsync(zaakInformatieObject, errors);
        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.InformatieObjectTypen.Contains(informatieObject.InformatieObjectType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.MissingZaakTypeInformatieObjectTypeRelation,
                "De referentie hoort niet bij het zaaktype van de zaak."
            );

            // TODO: Analyse issue: FUND-910 Error on ZT -IOT relation that does exist
            _logger.LogWarning("Request.informatieobject: {InformatieObject}", request.ZaakInformatieObject.InformatieObject);
            _logger.LogWarning("Request.zaak: {ZaakUrl}", request.ZaakUrl);

            _logger.LogWarning("InformatieObjectType: {InformatieObject}", informatieObject.InformatieObjectType);
            _logger.LogWarning("ZaakType: {ZaakType}", zaak.Zaaktype);
            _logger.LogWarning("ZaakType.InformatieObjectTypen: {zaakTypeInformatieObjectTypen}", string.Join(", ", zaakType.InformatieObjectTypen));
            // ----

            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            zaakInformatieObject.RegistratieDatum = DateTime.UtcNow;
            zaakInformatieObject.ZaakId = zaak.Id;
            zaakInformatieObject.Zaak = zaak;
            zaakInformatieObject.Owner = zaak.Owner;

            await _context.ZaakInformatieObjecten.AddAsync(zaakInformatieObject, cancellationToken);

            audittrail.SetNew<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            await audittrail.CreatedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakInformatieObject successfully created. Id={Id}", zaakInformatieObject.Id);
        }

        await SynchronizeObjectInformatieObjectInDrc(zaakInformatieObject, cancellationToken);

        await SendNotificationAsync(Actie.create, zaakInformatieObject, cancellationToken);

        return new CommandResult<ZaakInformatieObject>(zaakInformatieObject, CommandStatus.OK);
    }

    /*
        Test-flows

        timeout     asyncOnly     batch     result
        -------------------------------------------------------------------------------
        no          no            no        direct-call
        no          no            yes       queued with prio 1
        no          yes           no        queued with prio 2
        no          yes           yes       queued with prio 1
        yes         no            no        direct-call [timeout] => queued with prio 2
        yes         no            yes       queued with prio 1
        yes         yes           no        queued with prio 2
        yes         yes           yes       queued with prio 1
     */

    private async Task SynchronizeObjectInformatieObjectInDrc(ZaakInformatieObject zaakInformatieObject, CancellationToken cancellationToken)
    {
        bool asyncOnly = _applicationConfiguration.DrcSynchronizationAsyncOnlyMode;

        //
        // Note: During creation, the mirrored relationship is also created in the Documents API, but without the relationship information.

        bool timeout = false;
        if (!asyncOnly && string.IsNullOrEmpty(_batchIdAccessor.Id))
        {
            // Only non-batch messages should be called synchroneously
            try
            {
                // Non-batch messages should be handled first so try to handle synchroneously
                // Try to get ObjectInformatieObject synchronized into the DRC
                var response = await _client.GetResponse<AddObjectInformatieObjectResult>(
                    new
                    {
                        Object = _uriService.GetUri(zaakInformatieObject.Zaak),
                        zaakInformatieObject.InformatieObject,
                        ObjectType = "zaak",
                        Rsin = _rsin,
                        _correlationContextAccessor.CorrelationId,
                    },
                    timeout: _applicationConfiguration.DrcSynchronizationTimeoutSeconds * 1000
                );

                _logger.LogDebug(
                    "Successfully created the mirrored relationship into DRC. Url is {ObjectInformatieObjectUrl}",
                    response.Message.ObjectInformatieObjectUrl
                );
                return;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout error creating the mirrored relationship into DRC. Message is put on the queue and processed later.");

                timeout = true;
            }
        }

        if (!string.IsNullOrEmpty(_batchIdAccessor.Id) || asyncOnly || timeout)
        {
            byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

            // Handle it a-synchroneously in case of batch messages or in case of an error (timeout)
            await _publishEndpoint.Publish<IAddObjectInformatieObject>(
                new
                {
                    Object = _uriService.GetUri(zaakInformatieObject.Zaak),
                    zaakInformatieObject.InformatieObject,
                    ObjectType = "zaak",
                    Rsin = _rsin,
                    _correlationContextAccessor.CorrelationId,
                },
                context => context.SetPriority(priority),
                cancellationToken
            );
        }
    }

    private async Task<EnkelvoudigInformatieObjectResponseDto> GetInformatieObjectAsync(
        ZaakInformatieObject zaakInformatieObject,
        List<ValidationError> errors
    )
    {
        var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(zaakInformatieObject.InformatieObject);

        if (!result.Success)
        {
            errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private async Task<ZaakTypeResponseDto> GetZaakTypeAsync(string zaakTypeUrl, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaakTypeUrl);
        if (!result.Success)
        {
            errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" };
}

class CreateZaakInformatieObjectCommand : IRequest<CommandResult<ZaakInformatieObject>>
{
    public ZaakInformatieObject ZaakInformatieObject { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
