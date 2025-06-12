using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Asp.Versioning;
using AutoMapper.Internal;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.ErrorHandling;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

public class ZGWApiOptions
{
    public ZGWApiServiceSettings ApiServiceSettings { get; set; } = new();
    public Action<MvcNewtonsoftJsonOptions> NewtonsoftJsonOptions { get; set; } = null;
    public Action<MvcOptions> MvcOptions { get; set; } = null;
    public Action<AddSwaggerOptions> AddSwaggerOptions { get; set; } = null;
    public Action<SwaggerGenOptions> SwaggerGenOptions { get; set; } = null;
}

public class ZGWApiServiceSettings
{
    public bool RegisterSharedAudittrailHandlers = false;
    public bool UseVNGVersioning = true;
    public bool AddXmlSerializerOutputFormatter = false;
    public string ApiGroupNameFormat = "'v'VVV";
}

public static class ZGWApiServiceCollectionExtensions
{
    public static void AddZGWApi(
        this IServiceCollection services,
        string apiName,
        IConfiguration configuration,
        string defaultApiVersion,
        Action<ZGWApiOptions> configureZgwApiOptions = null
    )
    {
        var zgwApiOptions = new ZGWApiOptions();
        configureZgwApiOptions?.Invoke(zgwApiOptions);

        services.AddLocalization();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
            if (knownNetworks.Length != 0)
            {
                foreach (var network in knownNetworks)
                {
                    var parts = network.Split('/');
                    if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ipAddress) && int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ipAddress, prefixLength));
                    }
                    else
                    {
                        throw new FormatException($"Invalid network format in 'ForwardedHeaders:KnownNetworks': '{network}'. Expected format is '[IPAddress]/[PrefixLength]'.");
                    }
                }
            }

            var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
            if (knownProxies.Length != 0)
            {
                foreach (var proxy in knownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            }

            bool resolverForwardedHeader = configuration.GetValue("Application:ResolveForwardedHost", false);
            if (resolverForwardedHeader)
            {
                options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;
            }
        });

        var callingAssembly = Assembly.GetCallingAssembly();
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddHealthChecks();

        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssemblies(callingAssembly);
        });
        if (zgwApiOptions.ApiServiceSettings.RegisterSharedAudittrailHandlers)
        {
            services.AddMediatR(x =>
            {
                x.RegisterServicesFromAssemblies(typeof(LogAuditTrailGetObjectListCommand).GetTypeInfo().Assembly); // The shared command handlers for audittrail for some API's not all
            });
        }

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(HandlerLoggingBehavior<,>));

        services.AddAutoMapper(
            mappingConfiguration =>
            {
                mappingConfiguration.ShouldMapMethod = m => false;
                mappingConfiguration.Internal().Mappers.Insert(0, new NullableEnumMapper());
            },
            callingAssembly,
            executingAssembly
        );

        // Replace the default IApiVersionParser implementation with our own implementation which supports patch numbr (like 1.3.1)
        services.Replace(ServiceDescriptor.Transient<IApiVersionParser, ZgwApiVersionParser>());

        services
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                zgwApiOptions.NewtonsoftJsonOptions?.Invoke(options);
            });

        services
            .AddMvcCore(options =>
            {
                options.Filters.Add<ApiExceptionFilter>();
                options.Filters.Add<ValidationResultFilter>();

                // asp.net core model binding validation and NetTopologySuite geometry does not like each other,
                // so we ignore validation on Geometry type
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Geometry)));
                options.ModelBinderProviders.Insert(0, new GuidBinderProvider());
                options.ReturnHttpNotAcceptable = true;

                if (zgwApiOptions.ApiServiceSettings.AddXmlSerializerOutputFormatter)
                {
                    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                }

                zgwApiOptions.MvcOptions?.Invoke(options);
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create;
            })
            .AddFluentValidation(options =>
            {
                options.RegisterValidatorsFromAssembly(callingAssembly, lifetime: ServiceLifetime.Singleton);
                options.ValidatorOptions.PropertyNameResolver = PropertyNameResolver.Default;
            });

        services
            .AddApiVersioning(options =>
            {
                var forceDefaultApiVersion = configuration.GetValue<string>("Application:ForceDefaultApiVersion", null);
                defaultApiVersion = forceDefaultApiVersion ?? defaultApiVersion;

                options.ApiVersionReader = new ZgwHeaderApiVersionReader("Api-Version", defaultApiVersion);
                options.ReportApiVersions = false; // Note: we have customized the way on how current/supported versions are reported in ApiVersionMiddleware class
                options.UnsupportedApiVersionStatusCode = (int)HttpStatusCode.MethodNotAllowed; // Note: We override the default BadRequest into MethodNotAllowed so it behaves as before
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = zgwApiOptions.ApiServiceSettings.ApiGroupNameFormat;
                options.SubstituteApiVersionInUrl = false;
            });

        services.AddRouting(options => options.LowercaseUrls = true);

        var zgwVersion = ApplicationInformation.GetVersion();
        services.AddSwagger(
            apiName,
            zgwVersion,
            zgwApiOptions.ApiServiceSettings.UseVNGVersioning,
            zgwApiOptions.SwaggerGenOptions,
            zgwApiOptions.AddSwaggerOptions
        );
    }
}
