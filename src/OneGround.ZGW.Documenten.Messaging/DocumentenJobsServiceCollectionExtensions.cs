using System;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Documenten.Messaging;

public static class DocumentenJobsServiceCollectionExtensions
{
    public static void AddDocumentenJobs(this IServiceCollection services, Action<DocumentenJobsOptions> configureOptions)
    {
        services.AddOptions<DocumentenJobsOptions>().Configure(configureOptions).ValidateOnStart();

        services.AddSingleton<DocumentenHangfireConnectionFactory>();
    }
}
