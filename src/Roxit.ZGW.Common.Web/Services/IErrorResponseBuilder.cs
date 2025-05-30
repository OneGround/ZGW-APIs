using System.Collections.Generic;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Common.Web.Services;

/// <summary>
/// Builds different <see cref="IErrorResponse"/> instances.
/// </summary>
public interface IErrorResponseBuilder
{
    /// <summary>
    /// Wraps FluentValidation result to ZGW bad request response.
    /// </summary>
    BadRequestObjectResult BadRequest(params ValidationResult[] validationResult);

    /// <summary>
    /// Wraps validation errors to ZGW bad request response.
    /// </summary>
    BadRequestObjectResult BadRequest(
        IEnumerable<ValidationError> validationErrors,
        string title = "Invalid input.",
        string code = ErrorCode.Invalid
    );

    /// <summary>
    /// Wraps unknown JSON parsing errors to ZGW bad request response.
    /// </summary>
    BadRequestObjectResult BadRequest(ModelStateDictionary modelState);

    /// <summary>
    /// Returns ZGW not found response.
    /// </summary>
    NotFoundObjectResult NotFound();

    /// <summary>
    /// Returns ZGW not found response.
    /// </summary>
    NotFoundObjectResult NotFound(IEnumerable<ValidationError> validationErrors);

    /// <summary>
    /// Returns ZGW not found response for incorrect page.
    /// </summary>
    NotFoundObjectResult PageNotFound();

    /// <summary>
    /// Returns ZGW error 415 on bad Json.
    /// </summary>
    /// <param name="rootEntry"></param>
    /// <returns></returns>
    JsonResult InvalidJsonRequest(ModelStateEntry rootEntry);

    /// <summary>
    /// Returns 403 Forbidden response.
    /// </summary>
    /// <returns></returns>
    JsonResult Forbidden();

    /// <summary>
    /// Returns 403 Forbidden response with validation errors.
    /// </summary>
    /// <returns></returns>
    JsonResult Forbidden(IEnumerable<ValidationError> validationErrors);

    /// <summary>
    /// Returns 401 Unauthorized response.
    /// </summary>
    /// <returns></returns>
    JsonResult Unauthorized();

    /// <summary>
    /// Returns 405 Method not allowed.
    /// </summary>
    /// <returns></returns>
    JsonResult MethodNotAllowed();

    /// <summary>
    /// Returns 412 Preconditional Failed response.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    JsonResult PreconditionFailed(string message);

    /// <summary>
    /// Returns 406 Not Acceptable response.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    JsonResult NotAcceptable(string message);

    /// <summary>
    /// Returns 500 internal server error response.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    JsonResult InternalServerError(string message = "");
}
