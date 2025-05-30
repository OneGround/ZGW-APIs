using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Roxit.ZGW.Common.Web.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Roxit.ZGW.Common.Web.Swagger;

public class RequiresContentCrsHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId == null)
            return;

        // Note: Only Get and Head operations do have RequiresContentCrs attribute on theire controller actions
        if (!context.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(RequiresContentCrs)))
            return;

        operation.Parameters ??= [];

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "Content-Crs",
                In = ParameterLocation.Header,
                Description =
                    "Het 'Coordinate Reference System' (CRS) van de geometrie in de vraag (request body). Volgens de GeoJSON spec is WGS84 de default (EPSG:4326 is hetzelfde als WGS84).\r\n\r\nAvailable values : EPSG:4326.",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
            }
        );
    }
}
