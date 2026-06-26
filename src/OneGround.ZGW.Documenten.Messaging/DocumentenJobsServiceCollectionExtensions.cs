using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace OneGround.ZGW.Documenten.Messaging;

public static class DocumentenJobsServiceCollectionExtensions
{
    public static void AddDocumentenJobs(this IServiceCollection services, Action<DocumentenJobsOptions> configureOptions)
    {
        services.AddOptions<DocumentenJobsOptions>().Configure(configureOptions).ValidateOnStart();

        services.AddKeyedSingleton<NpgsqlDataSource>(HangfireServiceKeys.DataSource, (sp, _) =>
            new NpgsqlDataSourceBuilder(sp.GetRequiredService<IOptions<DocumentenJobsOptions>>().Value.ConnectionString).Build());

        services.AddSingleton<DocumentenHangfireConnectionFactory>();
    }
}
