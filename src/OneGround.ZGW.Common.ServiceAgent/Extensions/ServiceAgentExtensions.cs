using System;
using System.Net.Http;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.ServiceAgent.Authentication;
using OneGround.ZGW.Common.ServiceAgent.Caching;
using OneGround.ZGW.Common.ServiceAgent.Configuration;
using OneGround.ZGW.Common.Services;
using Polly;

namespace OneGround.ZGW.Common.ServiceAgent.Extensions;

public enum AuthorizationType
{
    ServiceAccount,
    UserAccount,
    AnonymousClient,
}

public static class ServiceAgentExtensions
{
    public static void AddServiceAgent<TClient, TImplementation>(
        this IServiceCollection services,
        string serviceRoleName,
        IConfiguration configuration,
        Action<CachingConfiguration<TClient>> caching = null,
        AuthorizationType authorizationType = AuthorizationType.ServiceAccount
    )
        where TClient : class
        where TImplementation : class, TClient
    {
        // Note: Needed to make the registration unique (for example we have two IDocumentServiceAgent implementations: one for v1 and one for v1.1)
        var agentImplementationName = typeof(TImplementation).FullName;

        services.AddScoped<BatchIdHandler>();
        RegisterZgwTokenClient(services, configuration);

        var httpClient = services
            .AddHttpClient<TClient, TImplementation>(agentImplementationName)
            .ConfigurePrimaryHttpMessageHandler(s =>
            {
                var handler = new HttpClientHandler();

                if (configuration.GetValue<bool>("Application:DontCheckServerValidation"))
                {
                    var serverCertificateValidator = s.GetService<IServerCertificateValidator>();
                    handler.ServerCertificateCustomValidationCallback = serverCertificateValidator.ValidateCertificate;
                }

                return handler;
            })
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddHttpMessageHandler<BatchIdHandler>();

        // Bind named ResiliencePipeline{ServiceRoleName} from configuration (custom setting) like: "ResiliencePipelineZTC"
        services.Configure<ResiliencePipelineOptions>(
            $"resilience-pipeline-{serviceRoleName}",
            configuration.GetSection($"ServiceAgentsConfig:{serviceRoleName}")
        );

        httpClient.AddResilienceHandler(
            $"resilience-pipeline-{serviceRoleName}",
            (builder, context) =>
            {
                // Enable dynamic reloads of this pipeline whenever the named ResiliencePipeline change
                context.EnableReloads<ResiliencePipelineOptions>($"resilience-pipeline-{serviceRoleName}");

                // Retrieve the named options
                var options = context.GetOptions<ResiliencePipelineOptions>($"resilience-pipeline-{serviceRoleName}");

                builder.AddRetry(options.Retry).AddTimeout(options.Timeout);
            }
        );

        if (authorizationType == AuthorizationType.ServiceAccount)
        {
            httpClient.AddHttpMessageHandler(services =>
            {
                return services.GetRequiredService<ServiceAgentAuthenticationHandlerFactory>().Create(serviceRoleName);
            });
            services.AddSingleton<ServiceAgentAuthenticationHandlerFactory>();
        }
        else if (authorizationType == AuthorizationType.UserAccount)
        {
            httpClient.AddHttpMessageHandler(services =>
            {
                return services.GetRequiredService<AuthorizedServiceAgentAuthenticationHandlerFactory>().Create();
            });
            services.AddSingleton<AuthorizedServiceAgentAuthenticationHandlerFactory>();
            services.AddSingleton<IClientJwtTokenContext, ClientJwtTokenContext>();
        }

        if (caching != null)
        {
            services.AddSingleton(s =>
            {
                var serviceEndpoints = s.GetService<IServiceDiscovery>();
                var configuration = new CachingConfiguration<TClient>(serviceEndpoints);
                caching(configuration);
                return configuration;
            });
            services.AddTransient<CachingHandler<TClient>>();
            httpClient.AddHttpMessageHandler<CachingHandler<TClient>>();
        }

        services.AddTransient<CorrelationIdHandler>();

        services.AddSingleton<IServiceAgentResponseBuilder, ServiceAgentResponseBuilder>();
    }

    private static void RegisterZgwTokenClient(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZgwAuthConfiguration>(configuration.GetSection("Auth"));
        services.AddMemoryCache();
        services.AddSingleton<IZgwTokenCacheService, ZgwTokenCacheService>();

        services.AddSingleton<IZgwAuthDiscoveryCache, ZgwAuthDiscoveryCache>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var authenticationConfiguration = provider.GetRequiredService<ZgwAuthConfiguration>();
            var discoveryCache = new DiscoveryCache(
                authenticationConfiguration.ZgwLegacyAuthProviderUrl,
                () => httpClientFactory.CreateClient(nameof(IZgwAuthDiscoveryCache))
            );

            return new ZgwAuthDiscoveryCache(discoveryCache);
        });

        services
            .AddHttpClient<IZgwTokenServiceAgent, ZgwTokenServiceAgent>()
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddResilienceHandler(
                "ZGW-IDP-Token-Resilience",
                builder =>
                {
                    builder.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 3,
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            Delay = TimeSpan.FromSeconds(1),
                        }
                    );
                }
            );
    }
}
