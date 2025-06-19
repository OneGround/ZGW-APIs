using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Filters;

public class OneGroundFluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public OneGroundFluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = (IValidator)_serviceProvider.GetService(validatorType);

            if (validator is null)
                continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argument.GetType());
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument);

            var result = await validator.ValidateAsync(validationContext);

            if (result is not { IsValid: false })
                continue;

            var errorResponseBuilder = _serviceProvider.GetRequiredService<IErrorResponseBuilder>();

            context.Result = errorResponseBuilder.BadRequest(result);

            return;
        }

        await next();
    }
}
