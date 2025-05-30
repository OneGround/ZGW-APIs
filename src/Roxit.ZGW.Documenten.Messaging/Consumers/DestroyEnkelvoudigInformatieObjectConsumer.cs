using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Documenten.Messaging.Contracts;
using Roxit.ZGW.Documenten.Messaging.Services;

namespace Roxit.ZGW.Documenten.Messaging.Consumers;

public class DestroyEnkelvoudigInformatieObjectConsumer : IConsumer<IDestroyEnkelvoudigInformatieObject>
{
    private readonly ILogger<DeleteObjectInformatieObjectConsumer> _logger;
    private readonly IEnkelvoudigInformatieObjectDeletionService _enkelvoudigInformatieObjectDeletionService;

    public DestroyEnkelvoudigInformatieObjectConsumer(
        ILogger<DeleteObjectInformatieObjectConsumer> logger,
        IEnkelvoudigInformatieObjectDeletionService enkelvoudigInformatieObjectDeletionService
    )
    {
        _logger = logger;
        _enkelvoudigInformatieObjectDeletionService = enkelvoudigInformatieObjectDeletionService;
    }

    public async Task Consume(ConsumeContext<IDestroyEnkelvoudigInformatieObject> context)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.CorrelationId, ["RSIN"] = context.Message.Rsin }))
        {
            ArgumentNullException.ThrowIfNull(context.Message, nameof(context.Message));

            _logger.LogDebug($"{nameof(DestroyEnkelvoudigInformatieObjectConsumer)}: Dispatching message from queue...");

            _logger.LogDebug("Destroying object: Deleting document {EnkelvoudigInformatieObjectUrl}", context.Message.EnkelvoudigInformatieObjectUrl);

            await _enkelvoudigInformatieObjectDeletionService.QueueAsync(
                new EnkelvoudigInformatieObjectDeletion
                {
                    Rsin = context.Message.Rsin,
                    EnkelvoudigInformatieObjectUrl = context.Message.EnkelvoudigInformatieObjectUrl,
                    ObjectUrl = context.Message.ObjectUrl,
                }
            );
        }
    }
}
