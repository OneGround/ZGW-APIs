using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Validations;

/// <summary>
/// Resource filter to validate JSON body parameters against allowed [JsonProperty] attributes.
/// Runs before model binding so the request body stream is still available.
/// </summary>
public class ValidateBodyParametersFilter<TBodyDto> : IAsyncResourceFilter
    where TBodyDto : class
{
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    private static readonly HashSet<string> AllowedBodyParameters = GetAllowedBodyParameters();

    public ValidateBodyParametersFilter(IErrorResponseBuilder errorResponseBuilder)
    {
        _errorResponseBuilder = errorResponseBuilder;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // Enable buffering so the body can be read here and again by model binding
        request.EnableBuffering();

        string body;
        using (var reader = new StreamReader(request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }

        // Reset position so model binding can read the body
        request.Body.Position = 0;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                string[] excludedQueryParameters = ["page", "expand", "ordering"];

                var jObject = JObject.Parse(body);
                var invalidParams = jObject
                    .Properties()
                    .Select(p => p.Name)
                    .Where(k => !excludedQueryParameters.Contains(k, StringComparer.OrdinalIgnoreCase)) // Exclude pagination/expand parameters
                    .Where(k => !AllowedBodyParameters.Contains(k))
                    .ToList();

                if (invalidParams.Any())
                {
                    context.Result = _errorResponseBuilder.BadRequest(
                        validationErrors: invalidParams
                            .Select(e => new ValidationError
                            {
                                Name = e,
                                Reason = "Invalid body parameter",
                                Code = ErrorCode.Invalid,
                            })
                            .ToList()
                    );
                    return;
                }
            }
            catch (JsonReaderException)
            {
                // Let the framework handle malformed JSON
            }
        }

        await next();
    }

    private static HashSet<string> GetAllowedBodyParameters()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var type = typeof(TBodyDto);
        foreach (var propertyName in type.GetProperties()
                     .Select(prop => prop.GetCustomAttribute<JsonPropertyAttribute>())
                     .Where(attr => attr != null && !string.IsNullOrWhiteSpace(attr.PropertyName))
                     .Select(attr => attr!.PropertyName))
        {
            set.Add(propertyName);
        }
        return set;
    }
}
