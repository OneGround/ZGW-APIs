using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Roxit.ZGW.Common.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IHostBuilder SetConfigurationProviders(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(
            (ctx, config) =>
            {
                //Some configuration files are pre-loaded so clear it first
                config.Sources.Clear();

                config.AddSharedAppSettings(ctx.HostingEnvironment);

                config
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (ctx.HostingEnvironment.IsLocal())
                {
                    config.AddUserSecrets(Assembly.GetEntryAssembly());
                }

                config.AddEnvironmentVariables();
            }
        );
        return hostBuilder;
    }

    private static void AddSharedAppSettings(this IConfigurationBuilder configurationBuilder, IHostEnvironment hostEnvironment)
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
