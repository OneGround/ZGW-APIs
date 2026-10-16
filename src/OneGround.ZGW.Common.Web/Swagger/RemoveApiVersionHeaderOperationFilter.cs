using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OneGround.ZGW.Common.Web.Swagger;

public class RemoveApiVersionHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            return;

        var apiVer = operation.Parameters.SingleOrDefault(p =>
            p.In == ParameterLocation.Header && string.Equals(p.Name, "api-version", System.StringComparison.OrdinalIgnoreCase)
        );
        if (apiVer != null)
        {
            operation.Parameters.Remove(apiVer);
        }
    }
}
