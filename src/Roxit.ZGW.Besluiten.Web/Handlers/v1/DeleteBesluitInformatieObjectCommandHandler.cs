using System;
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
using Roxit.ZGW.Besluiten.Web.Notificaties;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Messaging.Contracts;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class DeleteBesluitInformatieObjectCommandHandler
    : BesluitenBaseHandler<DeleteBesluitInformatieObjectCommandHandler>,
        IRequestHandler<DeleteBesluitInformatieObjectCommand, CommandResult>
{
    private readonly BrcDbContext _context;
    private readonly IRequestClient<IDeleteObjectInformatieObject> _client;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public DeleteBesluitInformatieObjectCommandHandler(
        ILogger<DeleteBesluitInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IRequestClient<IDeleteObjectInformatieObject> client,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        ICorrelationContextAccessor correlationContextAccessor,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBatchIdAccessor batchIdAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _client = client;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _correlationContextAccessor = correlationContextAccessor;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task<CommandResult> Handle(DeleteBesluitInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<BesluitInformatieObject>(o => o.Besluit.Owner == _rsin);

        var besluitInformatieObject = await _context
            .BesluitInformatieObjecten.Where(rsinFilter)
            .Include(z => z.Besluit.BesluitInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluitInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        // keep the reference for later, because saving context removed relation from besluitInformatieObject.Besluit
        var besluit = besluitInformatieObject.Besluit;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting BesluitInformatieObject {Id}....", besluitInformatieObject.Id);

            audittrail.SetOld<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

            _context.BesluitInformatieObjecten.Remove(besluitInformatieObject);

            await audittrail.DestroyedAsync(besluitInformatieObject.Besluit, besluitInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("BesluitInformatieObject {Id} successfully deleted.", besluitInformatieObject.Id);
        }

        await SynchronizeObjectInformatieObjectInDrc(besluit, besluitInformatieObject, cancellationToken);

        await SendNotificationAsync(Actie.destroy, besluitInformatieObject, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private async Task SynchronizeObjectInformatieObjectInDrc(
        Besluit besluit,
        BesluitInformatieObject besluitInformatieObject,
        CancellationToken cancellationToken
    )
    {
        bool asyncOnly = _applicationConfiguration.DrcSynchronizationAsyncOnlyMode;

        // Note: The mirrored relationship in the Documents API is removed by the Business API. Consumers cannot do this manually.

        bool timeout = false;
        if (!asyncOnly && string.IsNullOrEmpty(_batchIdAccessor.Id))
        {
            // Only non-batch messages should be called synchroneously
            try
            {
                // Non-batch messages should be handled first so try to handle synchroneously
                // Try to get ObjectInformatieObject synchronized into the DRC
                var response = await _client.GetResponse<DeleteObjectInformatieObjectResult>(
                    new
                    {
                        Object = _uriService.GetUri(besluit),
                        ObjectDestroy = true,
                        besluitInformatieObject.InformatieObject,
                        Rsin = _rsin,
                        _correlationContextAccessor.CorrelationId,
                    },
                    timeout: _applicationConfiguration.DrcSynchronizationTimeoutSeconds * 1000
                );

                _logger.LogDebug(
                    "Successfully removed the mirrored relationship from DRC. Url was {ObjectInformatieObjectUrl}",
                    response.Message.ObjectInformatieObjectUrl
                );
                return;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout error removing the mirrored relationship from DRC. Message is put on the queue and processed later.");

                timeout = true;
            }
        }

        if (!string.IsNullOrEmpty(_batchIdAccessor.Id) || asyncOnly || timeout)
        {
            byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

            // Handle it a-synchroneously in case of batch messages or in case of an error (timeout)
            await _publishEndpoint.Publish<IDeleteObjectInformatieObject>(
                new
                {
                    Object = _uriService.GetUri(besluit),
                    ObjectDestroy = true,
                    besluitInformatieObject.InformatieObject,
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

class DeleteBesluitInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
