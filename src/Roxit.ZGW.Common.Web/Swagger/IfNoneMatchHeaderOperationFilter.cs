using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Roxit.ZGW.Common.Web.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Roxit.ZGW.Common.Web.Swagger;

public class IfNoneMatchHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId == null)
            return;

        // Note: Only Get and Head operations do have ETag support with "If-None-Match" HTTP request header which is marked with ETagFilter attribute
        if (!context.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(ETagFilter)))
            return;

        operation.Parameters ??= [];

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "If-None-Match",
                In = ParameterLocation.Header,
                Description =
                    "Voer een voorwaardelijk verzoek uit. Deze header moet één of meerdere ETag-waardes bevatten van resources die de consumer gecached heeft. Indien de waarde van de ETag van de huidige resource voorkomt in deze set, dan antwoordt de provider met een lege HTTP 304 request. Zie MDN voor meer informatie.",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
            }
        );
    }
}
