using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Roxit.ZGW.Common.Web.Swagger;

public class RemoveApiVersionHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiVer = operation.Parameters.SingleOrDefault(p =>
            p.In == ParameterLocation.Header && p.Name.Equals("api-version", System.StringComparison.CurrentCultureIgnoreCase)
        );
        if (apiVer != null)
        {
            operation.Parameters.Remove(apiVer);
        }
    }
}
