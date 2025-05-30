using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using NetTopologySuite.Geometries;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Web.Swagger;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

public class VersionDescriptionDetails
{
    public string ApiName { get; set; }
    public ApiVersionDescription ApiVersionDescription { get; set; }
    public string ZgwVersion { get; set; }
    public bool UseVNGVersioning { get; set; }
}

public class AddSwaggerOptions
{
    public Func<VersionDescriptionDetails, string> NameBuilder { get; set; } = DefaultNameBuilder;
    public Func<VersionDescriptionDetails, string> TitleBuilder { get; set; } = DefaultTitleBuilder;
    public Func<VersionDescriptionDetails, string> DescriptionBuilder { get; set; } = DefaultDescriptionBuilder;

    private static string DefaultNameBuilder(VersionDescriptionDetails details)
    {
        return details.ApiVersionDescription.GroupName;
    }

    private static string DefaultTitleBuilder(VersionDescriptionDetails details)
    {
        return $"{details.ApiName} {details.ApiVersionDescription.ApiVersion} API";
    }

    private static string DefaultDescriptionBuilder(VersionDescriptionDetails details)
    {
        return $"ZGW Version: {details.ZgwVersion}{(details.UseVNGVersioning ? $" | VNG Version: {details.ApiVersionDescription.ApiVersion}" : "")}";
    }
}

public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddSwagger(
        this IServiceCollection services,
        string apiName,
        string zgwVersion,
        bool useVNGVersioning,
        Action<SwaggerGenOptions> swaggerGenOptions = null,
        Action<AddSwaggerOptions> configureAddSwaggerOptions = null
    )
    {
        var addSwaggerOptions = new AddSwaggerOptions();
        configureAddSwaggerOptions?.Invoke(addSwaggerOptions);

        services.AddSwaggerGen(x =>
        {
            // TODO: We have to make an OpenApi schema later. But it works for now
            x.MapType<Geometry>(() => new OpenApiSchema { Type = "object" });

            x.CustomOperationIds(SwaggerCustomOperationIdsSelector.OperationIdSelector);
            x.CustomSchemaIds(type => DefaultSchemaIdSelector(type));
            x.ExampleFilters();

            var apiVersionDescriptionProvider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var versionDescription in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                var details = new VersionDescriptionDetails()
                {
                    ApiName = apiName,
                    ApiVersionDescription = versionDescription,
                    UseVNGVersioning = useVNGVersioning,
                    ZgwVersion = zgwVersion,
                };

                x.SwaggerDoc(
                    addSwaggerOptions.NameBuilder(details),
                    new OpenApiInfo()
                    {
                        Title = addSwaggerOptions.TitleBuilder(details),
                        Version = zgwVersion,
                        Description = addSwaggerOptions.DescriptionBuilder(details),
                    }
                );
            }

            x.AddSecurityDefinition(SwaggerSecurityDefinitions.JWTBearerToken.Name, SwaggerSecurityDefinitions.JWTBearerToken.Scheme);
            x.AddSecurityRequirement(
                new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme { Reference = SwaggerSecurityDefinitions.JWTBearerToken.Reference },
                        new List<string>()
                    },
                }
            );

            var xmlFile = $"{ApplicationInformation.GetName()}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                x.IncludeXmlComments(xmlPath);
            }

            swaggerGenOptions?.Invoke(x);
        });

        services.AddSwaggerExamples();
        services.AddSwaggerGenNewtonsoftSupport();

        return services;
    }

    // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/752
    private static string DefaultSchemaIdSelector(Type modelType)
    {
        if (!modelType.IsConstructedGenericType)
            return modelType.ToString(); //Can also be modelType.Name

        var prefix = modelType
            .GetGenericArguments()
            .Select(genericArg => DefaultSchemaIdSelector(genericArg))
            .Aggregate((previous, current) => previous + current);

        return prefix + modelType.Name.Split('`').First();
    }
}
