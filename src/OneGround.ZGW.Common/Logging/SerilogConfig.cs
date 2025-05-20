using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace OneGround.ZGW.Common.Logging;

public static class SerilogConfig
{
    public static IHostBuilder UseAndConfigureSerilog(this IHostBuilder builder, string serviceRoleName, string logFileName)
    {
        builder.UseSerilog(
            (ctx, config) =>
            {
                var versionString = Assembly.GetAssembly(typeof(SerilogConfig))?.GetName().Version?.ToString();

                var outputTemplate = "[{Timestamp:HH:mm:ss.fff} {CorrelationId} {Level:u3}] {Message:lj}{NewLine}{Exception}";

                var logConfig = config
                    .ReadFrom.Configuration(ctx.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProperty("assembly_version", versionString)
                    .Enrich.WithProperty("role", serviceRoleName)
                    .Filter.ByExcluding(e =>
                    {
                        // don't log on /health requests unless it's warning or error
                        return e.Level < LogEventLevel.Warning
                            && e.Properties.TryGetValue("RequestPath", out var requestPath)
                            && requestPath.ToString().Equals(@"""/health""");
                    })
                    .Destructure.ToMaximumCollectionCount(3)
                    .Destructure.ToMaximumDepth(5)
                    .Destructure.ToMaximumStringLength(255)
                    .Destructure.With(new GeometryDeconstructingPolicy())
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: outputTemplate);

                if (!ctx.HostingEnvironment.IsLocal() && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var logPath = $"/var/log/app/{logFileName}";

                    logConfig.WriteTo.Async(a =>
                        a.File(
                            formatter: new JsonFormatter(renderMessage: true, formatProvider: CultureInfo.CurrentCulture),
                            path: logPath,
                            fileSizeLimitBytes: null,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 2,
                            shared: true
                        )
                    );
                }
            }
        );
        return builder;
    }
}
