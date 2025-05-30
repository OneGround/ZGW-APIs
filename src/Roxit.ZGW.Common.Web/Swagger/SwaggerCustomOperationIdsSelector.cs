using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Roxit.ZGW.Common.Web.Swagger;

public static class SwaggerCustomOperationIdsSelector
{
    public static string OperationIdSelector(ApiDescription apiDescription)
    {
        if (
            apiDescription.ActionDescriptor.RouteValues.TryGetValue("controller", out string controllerName)
            && apiDescription.ActionDescriptor.RouteValues.TryGetValue("action", out string actionName)
        )
        {
            return $"{controllerName.Replace("Controller", "")}{actionName.Replace("Async", "")}";
        }

        return null;
    }
}
