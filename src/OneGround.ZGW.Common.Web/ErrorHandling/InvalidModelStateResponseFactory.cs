using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.ErrorHandling;

public static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var responseBuilder = context.HttpContext.RequestServices.GetService<IErrorResponseBuilder>();
        if (!context.ModelState.IsValid)
        {
            if (context.ModelState.ContainsKey(string.Empty))
            {
                return responseBuilder.InvalidJsonRequest(context.ModelState.Root);
            }
        }

        return responseBuilder.BadRequest(context.ModelState);
    }
}
