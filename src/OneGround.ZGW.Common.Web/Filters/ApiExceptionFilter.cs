using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.Exceptions;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Filters;

public class ApiExceptionFilter : ExceptionFilterAttribute
{
    private readonly IErrorResponseBuilder _responseBuilder;

    public ApiExceptionFilter(IErrorResponseBuilder responseBuilder)
    {
        _responseBuilder = responseBuilder;
    }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is InvalidGeometryException geometryExeption)
        {
            context.ModelState.AddModelError(geometryExeption.PropertyName, geometryExeption.Message);
            context.Result = _responseBuilder.BadRequest(context.ModelState);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is ApiJsonParsingException jsonParsingException)
        {
            context.ModelState.Clear();
            context.ModelState.AddModelError(jsonParsingException.PropertyName, jsonParsingException.Message);
            context.Result = _responseBuilder.BadRequest(context.ModelState);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is DbUpdateConcurrencyException)
        {
            context.Result = _responseBuilder.Conflict();
            context.ExceptionHandled = true;
        }
        base.OnException(context);
    }
}
