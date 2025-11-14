using System.Linq;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;

public static class SwaggerApplicationBuilderExtensions
{
    public static void ConfigureZgwSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(option =>
        {
            option.RouteTemplate = "/api/{documentName}/schema/openapi.yaml";
        });
        app.UseSwagger(option =>
        {
            option.RouteTemplate = "/api/{documentName}/schema/openapi.json";
        });

        var apiVersionDescriptionProvider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwaggerUI(option =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                option.SwaggerEndpoint($"/api/{description.GroupName}/schema/openapi.yaml", description.GroupName);
            }

            option.ConfigObject.Urls = option.ConfigObject.Urls.OrderByDescending(x => x.Name);
        });
    }
}
