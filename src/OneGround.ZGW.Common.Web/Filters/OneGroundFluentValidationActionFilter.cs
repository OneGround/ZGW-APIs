using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Filters;

public class OneGroundFluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public OneGroundFluentValidationActionFilter(IServiceProvider serviceProvider, IErrorResponseBuilder errorResponseBuilder)
    {
        _serviceProvider = serviceProvider;
        _errorResponseBuilder = errorResponseBuilder;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.Count == 0)
        {
            await next();
            return;
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null)
                continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = (IValidator)_serviceProvider.GetService(validatorType);

            if (validator is null)
                continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument);

            var result = await validator.ValidateAsync(validationContext);

            if (result is not { IsValid: false })
                continue;

            context.Result = _errorResponseBuilder.BadRequest(result);

            return;
        }

        await next();
    }
}
