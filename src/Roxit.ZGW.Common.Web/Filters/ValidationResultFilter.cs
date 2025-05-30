using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Common.Web.Filters;

/// <summary>
/// Creates bad request response if HttpContext has ValidationResult set by <see cref="ValidatorInterceptor"/>.
/// </summary>
public class ValidationResultFilter : ActionFilterAttribute
{
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public ValidationResultFilter(IErrorResponseBuilder errorResponseBuilder)
    {
        _errorResponseBuilder = errorResponseBuilder;
    }

    public override void OnResultExecuting(ResultExecutingContext context)
    {
        // if respose already 404, ignore validation
        if (context.Result is NotFoundObjectResult)
            return;

        if (!context.HttpContext.Items.TryGetValue("ValidationResult", out var value))
            return;

        if (value is not ValidationResult result)
            return;

        context.Result = _errorResponseBuilder.BadRequest(result);
    }
}
