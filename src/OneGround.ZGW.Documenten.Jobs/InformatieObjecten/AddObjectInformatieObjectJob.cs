using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Jobs.Extensions;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace OneGround.ZGW.Documenten.Jobs.InformatieObjecten;

public class AddObjectInformatieObjectJob : InformatieObjectHandlerBase<AddObjectInformatieObjectJob>
{
    public AddObjectInformatieObjectJob(
        ILogger<AddObjectInformatieObjectJob> logger,
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
        context.WriteLineColored(ConsoleTextColor.Yellow, "Adding objectinformatieobject into DRC is in progress...");

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

            var objectInformatieObject = new ObjectInformatieObjectRequestDto
            {
                InformatieObject = informatieobject,
                Object = @object,
                ObjectType = objecttype,
            };

            var result = await _documentenServiceAgent.AddObjectInformatieObjectAsync(objectInformatieObject);
            if (!result.Success)
            {
                if (result.Error.InvalidParams.Any(e => e.Code == "unique"))
                {
                    context.WriteLineColored(
                        ConsoleTextColor.Red,
                        $"Warning: Synchronization to the DRC has failed. Combination of informatieobject and object already exists. Probably already added."
                    );
                    return;
                }
                else
                {
                    throw new InvalidOperationException("Failed to add objectinformatieobject into DRC. " + result.GetErrorsFromResponse());
                }
            }

            context.WriteLineColored(ConsoleTextColor.Yellow, "Objectinformatieobject successfully added into DRC.");
        }
    }
}
