using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using OneGround.ZGW.Common.Web.Expands;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OneGround.ZGW.Common.Web.Swagger;

public class ExpandQueryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId == null)
            return;

        // Note: We have only expand support in all operartion which are marked with the (custom) Expand attribute
        if (!context.MethodInfo.CustomAttributes.Any(a => a.AttributeType == typeof(Expand)))
            return;

        operation.Parameters ??= [];

        var expand = operation.Parameters.SingleOrDefault(p =>
            p.In == ParameterLocation.Query && p.Name.Equals("expand", System.StringComparison.CurrentCultureIgnoreCase)
        );
        if (expand != null)
        {
            operation.Parameters.Remove(expand);
        }

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "expand",
                In = ParameterLocation.Query,
                Description =
                    "Haal details van gelinkte resources direct op. Als je meerdere resources tegelijk wilt ophalen kun je deze scheiden met een komma. Voor het ophalen van resources die een laag dieper genest zijn wordt de punt-notatie gebruikt.",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
            }
        );
    }
}
