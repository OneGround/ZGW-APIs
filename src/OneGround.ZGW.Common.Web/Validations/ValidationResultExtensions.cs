using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Common.Web.Validations;

public static class ValidationResultExtensions
{
    public static List<ValidationError> ToValidationErrors(this ValidationResult validationResult)
    {
        return validationResult
            .Errors.Select(e => new ValidationError
            {
                Name = e.PropertyName,
                Code = e.ErrorCode,
                Reason = e.ErrorMessage,
            })
            .ToList();
    }
}
