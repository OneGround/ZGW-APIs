using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Web.Filters;

namespace Roxit.ZGW.Common.Web.Validations;

/// <summary>
/// Interceptor to catch FluentValidation validation result and pass it to HttpContext.
/// This is used in <see cref="ValidationResultFilter"/> to generate custom ZGW bad request response.
/// </summary>
public class ValidatorInterceptor : IValidatorInterceptor
{
    public ValidationResult AfterAspNetValidation(ActionContext actionContext, IValidationContext validationContext, ValidationResult result)
    {
        if (!result.IsValid)
        {
            if (actionContext.HttpContext.Items.ContainsKey("ValidationResult"))
            {
                var existingValidationResult = actionContext.HttpContext.Items["ValidationResult"] as ValidationResult;

                existingValidationResult.Errors.AddRange(result.Errors);
            }
            else
            {
                actionContext.HttpContext.Items.Add("ValidationResult", result);
            }
        }
        return result;
    }

    public IValidationContext BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
    {
        return commonContext;
    }
}
