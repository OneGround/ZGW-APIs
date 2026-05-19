using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services;

/// <summary>
/// Resource filter to validate query-parameters against allowed [FromQueryAttribute] attributes.
/// </summary>
public class ValidateQueryParametersFilter<TQueryParametersDto> : IAsyncResourceFilter
    where TQueryParametersDto : class
{
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    private static readonly HashSet<string> AllowedQueryParameters = GetAllowedQueryParameters();

    public ValidateQueryParametersFilter(IErrorResponseBuilder errorResponseBuilder)
    {
        _errorResponseBuilder = errorResponseBuilder;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        string[] excludedQueryParameters = ["page", "expand", "ordering"];

        var query = context.HttpContext.Request.Query;
        var invalidParams = query
            .Keys.Where(k => !excludedQueryParameters.Contains(k, StringComparer.OrdinalIgnoreCase)) // Exclude pagination/expand parameters
            .Where(k => !AllowedQueryParameters.Contains(k))
            .ToList();
        if (invalidParams.Any())
        {
            context.Result = _errorResponseBuilder.BadRequest(
                validationErrors: invalidParams
                    .Select(e => new ValidationError
                    {
                        Name = e,
                        Reason = "Invalid query parameter",
                        Code = ErrorCode.Invalid,
                    })
                    .ToList()
            );
            return;
        }

        await next();
    }

    private static HashSet<string> GetAllowedQueryParameters()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var type = typeof(TQueryParametersDto);
        foreach (var prop in type.GetProperties())
        {
            var attr = prop.GetCustomAttribute<FromQueryAttribute>();
            if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
            {
                set.Add(attr.Name);
            }
        }
        return set;
    }
}
