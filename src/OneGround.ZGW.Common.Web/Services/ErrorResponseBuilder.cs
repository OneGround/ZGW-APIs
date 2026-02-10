using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Common.Web.Services;

public class ErrorResponseBuilder : IErrorResponseBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private string BaseUrl => $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";

    public ErrorResponseBuilder(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public BadRequestObjectResult BadRequest(params ValidationResult[] validationResult)
    {
        var errors = validationResult.SelectMany(v => v.Errors).Select(MapValidationError);

        // Required because on PATCH, validations run twice. ZaakRequestValidator + PreMergeZaakRequestValidator
        errors = errors
            .GroupBy(g => new
            {
                g.Name,
                g.Code,
                g.Reason,
            })
            .Select(g => g.First());

        return BadRequest(errors);
    }

    public BadRequestObjectResult BadRequest(ModelStateDictionary modelStateDictionary)
    {
        var errors = modelStateDictionary
            .Where(s => s.Value.ValidationState == ModelValidationState.Invalid)
            .Select(s => new ValidationError(s.Key, ErrorCode.Other, GetError(s.Value.Errors.First())))
            .ToArray();

        return BadRequest(errors);
    }

    private static string GetError(ModelError modelError)
    {
        if (!string.IsNullOrEmpty(modelError.ErrorMessage))
            return modelError.ErrorMessage;

        if (modelError.Exception != null)
            return modelError.Exception.Message;

        return string.Empty;
    }

    public BadRequestObjectResult BadRequest(
        IEnumerable<ValidationError> validationErrors,
        string title = "Invalid input.",
        string code = ErrorCode.Invalid
    )
    {
        return new BadRequestObjectResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = code,
                Title = title,
                Status = (int)HttpStatusCode.BadRequest,
                InvalidParams = validationErrors.ToList(),
            }
        );
    }

    public NotFoundObjectResult NotFound()
    {
        return new NotFoundObjectResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.NotFound}",
                Code = ErrorCode.NotFound,
                Title = "Niet gevonden.",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "Niet gevonden.",
            }
        );
    }

    public NotFoundObjectResult NotFound(IEnumerable<ValidationError> validationErrors)
    {
        return new NotFoundObjectResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.NotFound}",
                Code = ErrorCode.NotFound,
                Title = "Niet gevonden.",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "Niet gevonden.",
                InvalidParams = validationErrors.ToList(),
            }
        );
    }

    public NotFoundObjectResult PageNotFound()
    {
        return new NotFoundObjectResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.NotFound,
                Title = "Niet gevonden.",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "Ongeldige pagina.",
            }
        );
    }

    public JsonResult Forbidden()
    {
        var statusCode = (int)HttpStatusCode.Forbidden;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.PermissionDenied,
                Title = "Toestemming geweigerd.",
                Status = statusCode,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult Forbidden(IEnumerable<ValidationError> validationErrors)
    {
        var statusCode = (int)HttpStatusCode.Forbidden;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.PermissionDenied,
                Title = "Toestemming geweigerd.",
                Status = statusCode,
                InvalidParams = validationErrors.ToList(),
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult Unauthorized()
    {
        var statusCode = (int)HttpStatusCode.Unauthorized;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.AuthenticationDenied,
                Title = "Authenticatie geweigerd.",
                Status = statusCode,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult MethodNotAllowed()
    {
        var statusCode = (int)HttpStatusCode.MethodNotAllowed;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.MethodNotAllowed,
                Title = "Method not allowed.",
                Status = statusCode,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult InvalidJsonRequest(ModelStateEntry root)
    {
        var statusCode = (int)HttpStatusCode.UnsupportedMediaType;
        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.UnsupportedMediaType}",
                Code = ErrorCode.UnsupportedMediaType,
                Title = "Ongeldige media type \"application/javascript\" in aanvraag.",
                Status = statusCode,
                Detail = root.Errors.FirstOrDefault()?.ErrorMessage,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult PreconditionFailed(string message)
    {
        var statusCode = (int)HttpStatusCode.PreconditionFailed;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.PreconditionFailed,
                Title = "Voorwaarde is niet vervuld",
                Status = statusCode,
                Detail = message,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult NotAcceptable(string message)
    {
        var statusCode = (int)HttpStatusCode.PreconditionFailed;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.NotAcceptable,
                Title = "Kan niet voldoen aan de opgegeven Accept header.",
                Status = statusCode,
                Detail = message,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult InternalServerError(string message)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.InternalServerError}",
                Code = ErrorCode.Other,
                Title = "Internal server error.",
                Status = statusCode,
                Detail = message,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult Conflict()
    {
        var statusCode = (int)HttpStatusCode.Conflict;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.Conflict,
                Title = "Resource is vergrendeld.",
                Status = statusCode,
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    public JsonResult Conflict(IEnumerable<ValidationError> validationErrors)
    {
        var statusCode = (int)HttpStatusCode.Conflict;

        return new JsonResult(
            new ErrorResponse
            {
                Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                Code = ErrorCode.Conflict,
                Title = "Resource is vergrendeld.",
                Status = statusCode,
                InvalidParams = validationErrors.ToList(),
            }
        )
        {
            StatusCode = statusCode,
        };
    }

    private static ValidationError MapValidationError(ValidationFailure error)
    {
        return new ValidationError(error.PropertyName, MapErrorCode(error), MapErrorMessage(error));
    }

    private static string MapErrorCode(ValidationFailure error)
    {
        return error.ErrorCode switch
        {
            "NotNullValidator" => ErrorCode.Required,
            "NotEmptyValidator" => ErrorCode.Blank,
            "MaximumLengthValidator" => ErrorCode.MaxLength,
            "StringEnumValidator" => ErrorCode.InvalidChoice,
            "InclusiveBetweenValidator" => ErrorCode.Invalid,
            _ => error.ErrorCode,
        };
    }

    private static string MapErrorMessage(ValidationFailure error)
    {
        switch (error.ErrorCode)
        {
            case "NotNullValidator":
                return "Dit veld is vereist.";
            case "NotEmptyValidator":
                return "Dit veld mag niet leeg zijn.";
            case "MaximumLengthValidator":
                var length = error.FormattedMessagePlaceholderValues["MaxLength"];
                return $"Zorg ervoor dat dit veld niet meer dan {length} karakters bevat.";
            case "StringEnumValidator":
                return $"\"{error.AttemptedValue}\" is een ongeldige keuze.";
            case "InclusiveBetweenValidator":
                var from = error.FormattedMessagePlaceholderValues["From"];
                var to = error.FormattedMessagePlaceholderValues["To"];
                return $"Zorg ervoor dat dit veld een waarde heeft tussen {from} en {to}";
            default:
                return error.ErrorMessage;
        }
    }
}
