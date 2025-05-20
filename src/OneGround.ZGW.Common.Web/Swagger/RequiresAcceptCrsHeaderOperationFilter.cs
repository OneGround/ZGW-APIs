using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using OneGround.ZGW.Common.Web.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OneGround.ZGW.Common.Web.Swagger;

public class RequiresAcceptCrsHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId == null)
            return;

        // Note: Only Get and Head operations do have RequiresAcceptCrs attribute on theire controller actions
        if (!context.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(RequiresAcceptCrs)))
            return;

        operation.Parameters ??= [];

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "Accept-Crs",
                In = ParameterLocation.Header,
                Description =
                    "Het gewenste 'Coordinate Reference System' (CRS) van de geometrie in het antwoord (response body). Volgens de GeoJSON spec is WGS84 de default (EPSG:4326 is hetzelfde als WGS84).",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
            }
        );
    }
}
