using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Documenten.Messaging.Contracts;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace OneGround.ZGW.Documenten.Messaging.Consumers;

public class DeleteObjectInformatieObjectConsumer : ConsumerBase<DeleteObjectInformatieObjectConsumer>, IConsumer<IDeleteObjectInformatieObject>
{
    private readonly IDocumentenServiceAgent _documentenServiceAgent;

    public DeleteObjectInformatieObjectConsumer(ILogger<DeleteObjectInformatieObjectConsumer> logger, IDocumentenServiceAgent documentenServiceAgent)
        : base(logger)
    {
        _documentenServiceAgent = documentenServiceAgent;
    }

    public async Task Consume(ConsumeContext<IDeleteObjectInformatieObject> context)
    {
        ArgumentNullException.ThrowIfNull(context.Message, nameof(context.Message));

        using (GetLoggingScope(context.Message, context.Message.CorrelationId))
        {
            Logger.LogDebug($"{nameof(DeleteObjectInformatieObjectConsumer)}: Dispatching message from queue...");

            var resultsGet = await _documentenServiceAgent.GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
                context.Message.InformatieObject,
                context.Message.Object
            );
            if (!resultsGet.Success)
            {
                Logger.LogError(
                    "Synchronization to the Documenten service has failed. ObjectInformatieObject to get: {Object}-{InformatieObject}.",
                    context.Message.Object,
                    context.Message.InformatieObject
                );

                throw new Exception($"Error while consuming DeleteObjectInformatieObject (get). MessageId: {context.MessageId}");
            }

            var objectInformatieObject = resultsGet.Response.SingleOrDefault();
            if (objectInformatieObject != null)
            {
                var resultDelete = await _documentenServiceAgent.DeleteObjectInformatieObjectByUrlAsync(objectInformatieObject.Url);
                if (!resultDelete.Success)
                {
                    Logger.LogError(
                        "Synchronization (relation) to the Documenten service has failed. ObjectInformatieObject to delete: {objectInformatieObjectUrl}.",
                        objectInformatieObject.Url
                    );

                    throw new Exception($"Error while consuming DeleteObjectInformatieObject (delete). MessageId: {context.MessageId}");
                }

                //TODO: review this commented out code
                // Delete document (including storage) only if there are no other object-references exists (other zaken and/or besluiten!!)
                //if (context.Message.ObjectDestroy)
                //{
                //    // Get all document-zaak and/or document-besluit references for this document (so filter on InformatieObject only, keeping object null)
                //    resultsGet = await documentenServiceAgent.GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(context.Message.InformatieObject, @object: null);
                //    if (!resultsGet.Success)
                //    {
                //        _logger.LogError("Destroying object: Synchronization (storage) to the Documenten service has failed. Objecten to get from: {InformatieObject}.", context.Message.InformatieObject);

                //        throw new Exception($"Destroying object: Error while consuming DeleteObjectInformatieObject (get). MessageId: {context.MessageId}");
                //    }

                //    // When no document-zaak and/or document-besluit references exists ór only the one which is deleted here mark it as deleted (put it into the 'to-delete' queue)
                //    int referenceCount = resultsGet.Response.Count();
                //    if (referenceCount == 0 ||
                //       (referenceCount == 1 && resultsGet.Response.Single().InformatieObject == context.Message.InformatieObject && resultsGet.Response.Single().Object == context.Message.Object))
                //    {
                //        _logger.LogInformation("Destroying object: Queued for deletion {InformatieObject}", context.Message.InformatieObject);

                //        // Let a separate queue (and consumer) do the (delayed) destroying of the enkelvoudiginformatieobject (with document-content)
                //        await context.Publish<IDestroyEnkelvoudigInformatieObject>(new
                //        {
                //            CorrelationId = context.Message.CorrelationId,
                //            Rsin = context.Message.Rsin,
                //            EnkelvoudigInformatieObjectUrl = context.Message.InformatieObject,
                //            ObjectUrl = context.Message.Object
                //        });
                //    }
                //    else
                //    {
                //        _logger.LogInformation("Destroying object: Keep document {InformatieObject} because {count} object-reference(s) still exists to another object(s)", context.Message.InformatieObject, referenceCount);
                //    }
                //}

                await context.RespondAsync(new DeleteObjectInformatieObjectResult(objectInformatieObject.Url));
            }
            else
            {
                Logger.LogWarning(
                    "Synchronization to the Documenten service has failed. Combination of informatieObject '{InformatieObject}' and object '{Object}' not found. Probably already removed.",
                    context.Message.InformatieObject,
                    context.Message.Object
                );
            }
        }
    }
}
