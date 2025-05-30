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
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.BusinessRules;
using Roxit.ZGW.Besluiten.Web.Notificaties;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Messaging.Contracts;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class CreateBesluitInformatieObjectCommandHandler
    : BesluitenBaseHandler<CreateBesluitInformatieObjectCommandHandler>,
        IRequestHandler<CreateBesluitInformatieObjectCommand, CommandResult<BesluitInformatieObject>>
{
    private readonly BrcDbContext _context;
    private readonly IBesluitInformatieObjectBusinessRuleService _besluitInformatieObjectBusinessRuleService;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IRequestClient<IAddObjectInformatieObject> _client;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public CreateBesluitInformatieObjectCommandHandler(
        ILogger<CreateBesluitInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        IBesluitInformatieObjectBusinessRuleService besluitInformatieObjectBusinessRuleService,
        ICorrelationContextAccessor correlationContextAccessor,
        INotificatieService notificatieService,
        IRequestClient<IAddObjectInformatieObject> client,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBatchIdAccessor batchIdAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _besluitInformatieObjectBusinessRuleService = besluitInformatieObjectBusinessRuleService;
        _correlationContextAccessor = correlationContextAccessor;
        _client = client;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task<CommandResult<BesluitInformatieObject>> Handle(
        CreateBesluitInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating BesluitInformatieObject and validating....");

        var besluitInformatieObject = request.BesluitInformatieObject;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context
            .Besluiten.Where(rsinFilter)
            .Include(z => z.BesluitInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.BesluitUrl), cancellationToken);

        if (besluit == null)
        {
            var error = new ValidationError("besluit", ErrorCode.Invalid, $"Besluit {request.BesluitUrl} is onbekend.");

            errors.Add(error);
        }
        else
        {
            await _besluitInformatieObjectBusinessRuleService.ValidateAsync(
                besluit,
                besluitInformatieObject,
                _applicationConfiguration.IgnoreInformatieObjectValidation,
                errors
            );
        }

        if (errors.Count != 0)
        {
            return new CommandResult<BesluitInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            besluitInformatieObject.BesluitId = besluit.Id;
            besluitInformatieObject.Besluit = besluit;
            besluitInformatieObject.Registratiedatum = DateOnly.FromDateTime(DateTime.Today);
            besluitInformatieObject.AardRelatie = AardReleatie.legt_vast; // Zetten van relatieinformatie op BesluitInformatieObject - resource(brc-004)
            besluitInformatieObject.Owner = besluit.Owner;

            await _context.BesluitInformatieObjecten.AddAsync(besluitInformatieObject, cancellationToken);

            audittrail.SetNew<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

            await audittrail.CreatedAsync(besluitInformatieObject.Besluit, besluitInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("BesluitInformatieObject {Id} successfully created.", besluitInformatieObject.Id);
        }

        await SynchronizeObjectInformatieObjectInDrc(besluitInformatieObject, cancellationToken);

        await SendNotificationAsync(Actie.create, besluitInformatieObject, cancellationToken);

        return new CommandResult<BesluitInformatieObject>(besluitInformatieObject, CommandStatus.OK);
    }

    private async Task SynchronizeObjectInformatieObjectInDrc(BesluitInformatieObject besluitInformatieObject, CancellationToken cancellationToken)
    {
        bool asyncOnly = _applicationConfiguration.DrcSynchronizationAsyncOnlyMode;

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
                        Object = _uriService.GetUri(besluitInformatieObject.Besluit),
                        besluitInformatieObject.InformatieObject,
                        ObjectType = "besluit",
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
                    Object = _uriService.GetUri(besluitInformatieObject.Besluit),
                    besluitInformatieObject.InformatieObject,
                    ObjectType = "besluit",
                    Rsin = _rsin,
                    _correlationContextAccessor.CorrelationId,
                },
                context => context.SetPriority(priority),
                cancellationToken
            );
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluitinformatieobject" };
}

class CreateBesluitInformatieObjectCommand : IRequest<CommandResult<BesluitInformatieObject>>
{
    public BesluitInformatieObject BesluitInformatieObject { get; internal set; }
    public string BesluitUrl { get; internal set; }
}
