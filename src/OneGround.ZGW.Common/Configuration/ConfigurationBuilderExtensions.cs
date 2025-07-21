using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace OneGround.ZGW.Common.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IHostApplicationBuilder SetConfigurationProviders(this IHostApplicationBuilder builder)
    {
        //Some configuration files are pre-loaded so clear it first
        builder.Configuration.Sources.Clear();

        builder.Configuration.AddSharedAppSettings(builder.Environment);

        builder
            .Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        if (builder.Environment.IsLocal())
        {
            builder.Configuration.AddUserSecrets(Assembly.GetEntryAssembly());
        }

        builder.Configuration.AddEnvironmentVariables();
        return builder;
    }

    private static void AddSharedAppSettings(this IConfigurationManager configurationBuilder, IHostEnvironment hostEnvironment)
    {
        // load appsettings.Shared.json from the output folder in debug mode (IDE)
        // Since it is a (shared) linked file, and .net reads appsettings files directly from the source working folder,
        // We need to indicate specifically in debug mode that it reads from the output folder (bin/debug).
        if (hostEnvironment.IsLocal())
        {
            var outputFolderJson = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "appsettings.Shared.json");
            configurationBuilder.AddJsonFile(outputFolderJson, optional: true, reloadOnChange: false);
        }

        configurationBuilder.AddJsonFile("appsettings.Shared.json", optional: true, reloadOnChange: false);
    }
}
