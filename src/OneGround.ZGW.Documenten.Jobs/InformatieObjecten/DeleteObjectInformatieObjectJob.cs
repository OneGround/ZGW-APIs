using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Documenten.Jobs.Extensions;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace OneGround.ZGW.Documenten.Jobs.InformatieObjecten;

public class DeleteObjectInformatieObjectJob : InformatieObjectHandlerBase<DeleteObjectInformatieObjectJob>
{
    public DeleteObjectInformatieObjectJob(
        ILogger<DeleteObjectInformatieObjectJob> logger,
        IDocumentenServiceAgent documentenServiceAgent,
        IOrganisationContextFactory organisationContextFactory
    )
        : base(logger, documentenServiceAgent, organisationContextFactory) { }

    public async Task ExecuteAsync(
        string rsin,
        string @object,
        Guid correlationId,
        Guid? batchId,
        KeyValuePair<string, string> informatieObjectKenmerk,
        PerformContext context
    )
    {
        context.WriteLineColored(ConsoleTextColor.Yellow, "Deleting objectinformatieobject from DRC is in progress...");

        ArgumentNullException.ThrowIfNull(rsin, nameof(rsin));
        ArgumentNullException.ThrowIfNull(@object, nameof(@object));

        // Note: Set the OrganisationContext for the current request (so the correct authentication takes place in ServiceAgents)
        _organisationContextFactory.Create(rsin);

        using (GetLoggingScope(rsin, correlationId))
        {
            var (objecttype, informatieobject) = GetInformatieObject(informatieObjectKenmerk);

            if (batchId.HasValue)
            {
                context.WriteLineColored(ConsoleTextColor.Yellow, $">batchid: {batchId.Value}");
            }
            context.WriteLineColored(ConsoleTextColor.Yellow, $">objecttype: '{objecttype}'");
            context.WriteLineColored(ConsoleTextColor.Yellow, $">object: {@object}");
            context.WriteLineColored(ConsoleTextColor.Yellow, $">informatieobject: {informatieobject}");

            var objectinformatieobjecten = await _documentenServiceAgent.GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
                informatieObject: informatieobject,
                @object: @object
            );
            if (!objectinformatieobjecten.Success)
            {
                throw new InvalidOperationException("Failed to retrieve objectinformatieobjects from DRC.");
            }

            // Note: On one Object (zaak or besluit) only one informatieobject relation exists (unique index on informatieobjecten)
            var resultInformatieObjecten = objectinformatieobjecten.Response.SingleOrDefault(i => i.InformatieObject == informatieobject);
            if (resultInformatieObjecten != null)
            {
                var resultDelete = await _documentenServiceAgent.DeleteObjectInformatieObjectByUrlAsync(resultInformatieObjecten.Url);
                if (!resultDelete.Success)
                {
                    throw new InvalidOperationException("Failed to delete objectinformatieobject from DRC." + resultDelete.GetErrorsFromResponse());
                }
            }
            else
            {
                context.WriteLineColored(
                    ConsoleTextColor.Red,
                    $"Warning: Synchronization to the DRC has failed. Combination of informatieobject and object not found. Probably already removed."
                );

                return;
            }

            context.WriteLineColored(ConsoleTextColor.Yellow, "Successfully deleted objectinformatieobject from DRC.");
        }
    }
}
