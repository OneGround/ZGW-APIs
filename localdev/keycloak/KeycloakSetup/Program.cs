using KeycloakSetup.Configuration;
using KeycloakSetup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace KeycloakSetup
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            builder.Services.AddSerilog();

            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();

            // Configure services
            builder.Services.Configure<KeycloakSettings>(builder.Configuration.GetSection("Keycloak"));
            builder.Services.AddTransient<BadRequestLoggingDelegatingHandler>();
            builder.Services.AddScoped<KeycloakAccessTokenProvider>();
            
            builder.Services.AddHttpClient<KeycloakAccessTokenProvider>()
                .AddHttpMessageHandler<BadRequestLoggingDelegatingHandler>();
            builder.Services.AddHttpClient<KeycloakSetupService>()
                .AddHttpMessageHandler<BadRequestLoggingDelegatingHandler>();

            var host = builder.Build();

            // Run the setup
            using var scope = host.Services.CreateScope();
            var setupService = scope.ServiceProvider.GetRequiredService<KeycloakSetupService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting Keycloak Setup Tool");

            var success = await setupService.SetupKeycloakAsync();

            if (success)
            {
                logger.LogInformation("Keycloak setup completed successfully!");
                return 0;
            }

            logger.LogError("Keycloak setup failed. Check the logs for details.");
            return 1;
        }
    }
}