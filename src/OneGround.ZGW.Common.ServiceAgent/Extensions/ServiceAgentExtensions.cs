using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.ServiceAgent.Authentication;
using OneGround.ZGW.Common.ServiceAgent.Caching;
using OneGround.ZGW.Common.ServiceAgent.Configuration;
using OneGround.ZGW.Common.Services;
using Polly;

namespace OneGround.ZGW.Common.ServiceAgent.Extensions;

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

        var httpClient = services
            .AddHttpClient<TClient, TImplementation>(agentImplementationName!)
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
            httpClient.AddHttpMessageHandler(sp => sp.GetRequiredService<ServiceAgentAuthenticationHandlerFactory>().Create(serviceRoleName));
            services.AddSingleton<ServiceAgentAuthenticationHandlerFactory>();
        }
        else if (authorizationType == AuthorizationType.UserAccount)
        {
            httpClient.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizedServiceAgentAuthenticationHandlerFactory>().Create());
            services.AddSingleton<AuthorizedServiceAgentAuthenticationHandlerFactory>();
            services.AddSingleton<IClientJwtTokenContext, ClientJwtTokenContext>();
        }

        if (caching != null)
        {
            services.AddSingleton(s =>
            {
                var serviceEndpoints = s.GetService<IServiceDiscovery>();
                var cachingConfiguration = new CachingConfiguration<TClient>(serviceEndpoints);
                caching(cachingConfiguration);
                return cachingConfiguration;
            });
            services.AddTransient<CachingHandler<TClient>>();
            httpClient.AddHttpMessageHandler<CachingHandler<TClient>>();
        }

        services.AddTransient<CorrelationIdHandler>();

        services.AddSingleton<IServiceAgentResponseBuilder, ServiceAgentResponseBuilder>();
    }
}
