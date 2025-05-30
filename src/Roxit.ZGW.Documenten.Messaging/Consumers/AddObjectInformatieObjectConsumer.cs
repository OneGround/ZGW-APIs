using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.Messaging.Contracts;
using Roxit.ZGW.Documenten.ServiceAgent.v1;

namespace Roxit.ZGW.Documenten.Messaging.Consumers;

public class AddObjectInformatieObjectConsumer : ConsumerBase<AddObjectInformatieObjectConsumer>, IConsumer<IAddObjectInformatieObject>
{
    private readonly IDocumentenServiceAgent _documentenServiceAgent;

    public AddObjectInformatieObjectConsumer(ILogger<AddObjectInformatieObjectConsumer> logger, IDocumentenServiceAgent documentenServiceAgent)
        : base(logger)
    {
        _documentenServiceAgent = documentenServiceAgent;
    }

    public async Task Consume(ConsumeContext<IAddObjectInformatieObject> context)
    {
        ArgumentNullException.ThrowIfNull(context.Message, nameof(context.Message));

        using (GetLoggingScope(context.Message, context.Message.CorrelationId))
        {
            Logger.LogDebug("{AddObjectInformatieObjectConsumer}: Dispatching message from queue...", nameof(AddObjectInformatieObjectConsumer));

            var objectType = context.Message.ObjectType ?? GetObjectTypeFallback(context.Message.Object);

            var objectInformatieObject = new ObjectInformatieObjectRequestDto
            {
                InformatieObject = context.Message.InformatieObject,
                Object = context.Message.Object,
                ObjectType = objectType,
            };

            var result = await _documentenServiceAgent.AddObjectInformatieObjectAsync(objectInformatieObject);
            if (!result.Success)
            {
                if (result.Error.InvalidParams.Any(e => e.Code == "unique"))
                {
                    Logger.LogWarning(
                        "ObjectInformatieObject already exist: {Object}-{InformatieObject}-{ObjectType}.",
                        context.Message.Object,
                        context.Message.InformatieObject,
                        objectType
                    );

                    await context.RespondAsync(new AddObjectInformatieObjectResult(null));
                }
                else
                {
                    Logger.LogError(
                        "Synchronization to the Documenten service has failed. ObjectInformatieObject to add: {Object}-{InformatieObject}-{ObjectType}.",
                        context.Message.Object,
                        context.Message.InformatieObject,
                        objectType
                    );

                    throw new Exception($"Error while consuming AddObjectInformatieObject. MessageId: {context.MessageId}");
                }
            }
            else
            {
                await context.RespondAsync(new AddObjectInformatieObjectResult(result.Response.Url));
            }
        }
    }

    private string GetObjectTypeFallback(string @object)
    {
        if (@object == null)
            throw new NullReferenceException(nameof(@object));

        Logger.LogWarning("Fallback: Extracting ObjectType from Object url {url}", @object);

        if (@object.Contains("zaken"))
            return "zaak";

        if (@object.Contains("besluiten"))
            return "besluit";

        throw new InvalidOperationException($"Could not extract ObjectType from Object url ({@object}).");
    }
}
