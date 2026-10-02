using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.IntegrationTests.Common.Authorization;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using OneGround.ZGW.IntegrationTests.Common.Http;

namespace OneGround.ZGW.IntegrationTests.Common;

/// <summary>
/// Boots a ZGW API in-process against the test containers, with only authentication and outbound HTTP substituted.
/// </summary>
public abstract class ZgwWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    public const string EnvironmentName = "IntegrationTest";

    private const string IdentityProviderAuthority = "https://idp.integrationtest.invalid";
    private const string AutorisatiesApiUrl = "https://autorisaties.integrationtest.invalid/api/v1";

    // Program.cs rebuilds configuration before Startup reads it, so settings are also passed as process-wide environment variables.
    private static readonly object HostCreationLock = new();

    private readonly Dictionary<string, string> _settings;

    protected ZgwWebApplicationFactory(IntegrationTestContainers containers)
    {
        _settings = CreateSettings(containers);
        OutboundHttp.Responder = request => AutorisatiesApi.TryRespond(request) ?? TryRespondAsIdentityProvider(request);
    }

    public StubAutorisatiesApi AutorisatiesApi { get; } = new(AutorisatiesApiUrl);

    public StubOutboundHttp OutboundHttp { get; } = new();

    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));

        builder.ConfigureTestServices(services =>
        {
            services.AddTestAuthentication();
            services.AddStubOutboundHttp(OutboundHttp);

            ConfigureTestServices(services);
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

    protected static void RemoveHostedService<THostedService>(IServiceCollection services)
        where THostedService : IHostedService
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(THostedService)).ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static HttpResponseMessage TryRespondAsIdentityProvider(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        if (!uri.GetLeftPart(UriPartial.Authority).Equals(IdentityProviderAuthority, StringComparison.OrdinalIgnoreCase))
            return null;

        var json = uri.AbsolutePath switch
        {
            "/.well-known/openid-configuration" =>
                $$"""{"issuer":"{{IdentityProviderAuthority}}","token_endpoint":"{{IdentityProviderAuthority}}/connect/token","jwks_uri":"{{IdentityProviderAuthority}}/.well-known/jwks"}""",
            "/.well-known/jwks" => """{"keys":[]}""",
            "/connect/token" => """{"access_token":"integration-test","token_type":"Bearer","expires_in":3600}""",
            _ => null,
        };

        if (json == null)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static string ToEnvironmentVariableName(string key) => key.Replace(":", "__");

    private static Dictionary<string, string> CreateSettings(IntegrationTestContainers containers)
    {
        var settings = new Dictionary<string, string>
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
            ["Auth:Authority"] = IdentityProviderAuthority,
            ["Auth:ValidIssuer"] = IdentityProviderAuthority,
            ["Auth:ValidAudience"] = "integration-test",
            ["Auth:ZgwTokenIntrospectionEndpoint"] = $"{IdentityProviderAuthority}/introspect",
            ["Services:ZTC:Api"] = "https://catalogi.integrationtest.invalid/api/v1",
            ["Services:ZRC:Api"] = "https://zaken.integrationtest.invalid/api/v1",
            ["Services:AC:Api"] = AutorisatiesApiUrl,
            ["Services:BRC:Api"] = "https://besluiten.integrationtest.invalid/api/v1",
            ["Services:DRC:Api"] = "https://documenten.integrationtest.invalid/api/v1",
            ["Services:NRC:Api"] = "https://notificaties.integrationtest.invalid/api/v1",
            ["Services:RL:Api"] = "https://referentielijsten.integrationtest.invalid/api/v1",
            ["Serilog:MinimumLevel:Default"] = "Warning",
        };

        for (var i = 0; i < TestRsins.All.Length; i++)
        {
            settings[$"ZgwServiceAccounts:Credentials:{i}:Rsin"] = TestRsins.All[i];
            settings[$"ZgwServiceAccounts:Credentials:{i}:ClientId"] = "integration-test";
            settings[$"ZgwServiceAccounts:Credentials:{i}:ClientSecret"] = "integration-test";
        }

        return settings;
    }
}

/// <summary>
/// RvIG-reserved test numbers with a zero elfproef sum, which the specification rejects, so none can be a real organisation's RSIN.
/// </summary>
public static class TestRsins
{
    public const string A = "000000012";
    public const string B = "000000024";
    public const string C = "000000036";
    public const string D = "000000048";
    public const string E = "000000103";
    public const string F = "000000115";
    public const string G = "000000127";

    public static readonly string[] All = [A, B, C, D, E, F, G];
}
