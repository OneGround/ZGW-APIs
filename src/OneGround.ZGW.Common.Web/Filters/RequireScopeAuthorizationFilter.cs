using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Filters;

/// <summary>
/// Global authorization filter which makes a missing scope check fail closed: a controller action that carries neither
/// a <see cref="BaseScopeAttribute"/> nor an explicit <see cref="ScopeNotRequiredAttribute"/> is denied with a 403
/// instead of executing unprotected. Registered once in AddZGWApi, so it covers every API host.
/// </summary>
public class RequireScopeAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<RequireScopeAuthorizationFilter> _logger;
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public RequireScopeAuthorizationFilter(ILogger<RequireScopeAuthorizationFilter> logger, IErrorResponseBuilder errorResponseBuilder)
    {
        _logger = logger;
        _errorResponseBuilder = errorResponseBuilder;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Only MVC controller actions are in scope here; minimal-API and middleware endpoints never reach an MVC filter.
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return Task.CompletedTask;

        if (ScopeRequirement.IsSatisfiedBy(descriptor.ControllerTypeInfo, descriptor.MethodInfo))
            return Task.CompletedTask;

        _logger.LogError(
            "Action {Controller}.{Action} carries no scope attribute and is not marked ScopeNotRequired; denying the request with 403.",
            descriptor.ControllerTypeInfo.Name,
            descriptor.MethodInfo.Name
        );

        context.Result = _errorResponseBuilder.Forbidden();

        return Task.CompletedTask;
    }
}
