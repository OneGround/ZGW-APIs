using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using OneGround.ZGW.Common.Web.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Interceptors;

namespace OneGround.ZGW.Common.Web.Validations;

/// <summary>
/// Interceptor to catch FluentValidation validation result and pass it to HttpContext.
/// This is used in <see cref="ValidationResultFilter"/> to generate custom ZGW bad request response.
/// </summary>
public class ValidatorInterceptor : IValidatorInterceptor
{
    public IValidationContext BeforeValidation(ActionExecutingContext actionExecutingContext, IValidationContext validationContext)
    {
        return validationContext;
    }

    public ValidationResult AfterValidation(ActionExecutingContext actionExecutingContext, IValidationContext validationContext)
    {
        var result = new ValidationResult();

        if (actionExecutingContext.ModelState.IsValid)
        {
            return result;
        }

        if (actionExecutingContext.HttpContext.Items.TryGetValue("ValidationResult", out var item))
        {
            var existingValidationResult = item as ValidationResult;
            existingValidationResult?.Errors.AddRange(result.Errors);
        }
        else
        {
            actionExecutingContext.HttpContext.Items.Add("ValidationResult", result);
        }

        return result;
    }
}
