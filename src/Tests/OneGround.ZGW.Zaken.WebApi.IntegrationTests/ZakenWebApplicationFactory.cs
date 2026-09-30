using System;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.IntegrationTests.Common.Authorization;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using OneGround.ZGW.IntegrationTests.Common.Http;
using OneGround.ZGW.Zaken.WebApi.BackgroundServices;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Boots the Zaken API in-process against the test containers.
/// <para>
/// Substituted: configuration, the RabbitMQ bus (MassTransit in-memory test harness), outbound HTTP (<see cref="OutboundHttp"/>),
/// the authentication scheme (<see cref="TestAuthenticationHandler"/>) and the <c>IAuthorizationResolver</c>
/// (<see cref="AuthorizationResolver"/>), and the <see cref="InpBsnBackfillService"/> is not started.
/// </para>
/// <para>
/// Not substituted, on purpose: the <c>[Authorize]</c> attribute, the scope filters, the authorization context and the
/// MediatR handlers run as they do in production, and the schema is created by the ZrcDbContext migrations through the
/// API's own database initializer. These tests exist to exercise exactly that code.
/// </para>
/// </summary>
public sealed class ZakenWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string EnvironmentName = "IntegrationTest";

    // Program.cs clears the configuration sources and adds its own (ending with environment variables) before any
    // service is registered, and Startup reads the connection strings while it registers services. Configuration added
    // here with ConfigureAppConfiguration only arrives after that, so the connection strings and every other setting are
    // also passed as environment variables for the duration of CreateHost. Environment variables are process-wide, so
    // host creation is serialized.
    private static readonly object HostCreationLock = new();

    private readonly Dictionary<string, string> _settings;

    public ZakenWebApplicationFactory(IntegrationTestContainers containers)
    {
        _settings = CreateSettings(containers);
    }

    public StubAuthorizationResolver AuthorizationResolver { get; } = new();

    public StubOutboundHttp OutboundHttp { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));

        builder.ConfigureTestServices(services =>
        {
            services.AddTestAuthentication();
            services.AddStubAuthorizationResolver(AuthorizationResolver);
            services.AddStubOutboundHttp(OutboundHttp);

            // Replaces the RabbitMQ bus registered by Startup with the in-memory transport
            services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());

            RemoveHostedService<InpBsnBackfillService>(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (HostCreationLock)
        {
            var previous = _settings.Keys.ToDictionary(key => key, key => Environment.GetEnvironmentVariable(ToEnvironmentVariableName(key)));
            try
            {
                foreach (var (key, value) in _settings)
                {
                    Environment.SetEnvironmentVariable(ToEnvironmentVariableName(key), value);
                }

                return base.CreateHost(builder);
            }
            finally
            {
                foreach (var (key, value) in previous)
                {
                    Environment.SetEnvironmentVariable(ToEnvironmentVariableName(key), value);
                }
            }
        }
    }

    private static void RemoveHostedService<THostedService>(IServiceCollection services)
        where THostedService : IHostedService
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(THostedService)).ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static string ToEnvironmentVariableName(string key) => key.Replace(":", "__");

    private static Dictionary<string, string> CreateSettings(IntegrationTestContainers containers)
    {
        // Made-up values only: hosts use the reserved .invalid top-level domain, so nothing can resolve them
        return new Dictionary<string, string>
        {
            ["ConnectionStrings:UserConnectionString"] = containers.PostgreSqlConnectionString,
            ["ConnectionStrings:AdminConnectionString"] = containers.PostgreSqlConnectionString,
            ["ConnectionStrings:DataProtectionConnectionString"] = containers.PostgreSqlConnectionString,
            ["Redis:ConnectionString"] = containers.RedisConnectionString,
            ["Application:SkipMigrationsAtStartup"] = "false",
            ["HmacHasher:Latest"] = "v1",
            ["HmacHasher:HmacKey"] = "aW50ZWdyYXRpb24tdGVzdHMtb25seS1obWFjLWtleS1ub3QtYS1zZWNyZXQ=",
            ["Eventbus:HostName"] = "eventbus.integrationtest.invalid",
            ["Eventbus:VirtualHost"] = "/",
            ["Eventbus:UserName"] = "integration-test",
            ["Eventbus:Password"] = "integration-test",
            ["Auth:Authority"] = "https://idp.integrationtest.invalid/",
            ["Auth:ValidIssuer"] = "https://idp.integrationtest.invalid",
            ["Auth:ValidAudience"] = "integration-test",
            ["Auth:ZgwTokenIntrospectionEndpoint"] = "https://idp.integrationtest.invalid/introspect",
            ["ZgwServiceAccounts:Credentials:0:Rsin"] = "000000000",
            ["ZgwServiceAccounts:Credentials:0:ClientId"] = "integration-test",
            ["ZgwServiceAccounts:Credentials:0:ClientSecret"] = "integration-test",
            ["Services:ZTC:Api"] = "https://catalogi.integrationtest.invalid/api/v1",
            ["Services:ZRC:Api"] = "https://zaken.integrationtest.invalid/api/v1",
            ["Services:AC:Api"] = "https://autorisaties.integrationtest.invalid/api/v1",
            ["Services:BRC:Api"] = "https://besluiten.integrationtest.invalid/api/v1",
            ["Services:DRC:Api"] = "https://documenten.integrationtest.invalid/api/v1",
            ["Services:NRC:Api"] = "https://notificaties.integrationtest.invalid/api/v1",
            ["Services:RL:Api"] = "https://referentielijsten.integrationtest.invalid/api/v1",
            ["Serilog:MinimumLevel:Default"] = "Warning",
        };
    }
}
