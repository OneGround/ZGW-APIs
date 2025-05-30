using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Roxit.ZGW.Common.Web.Swagger;

public class DeprecatedOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        var apiDescription = context.ApiDescription;
        if (apiDescription.IsDeprecated())
        {
            operation.Deprecated = true;
        }
    }
}
