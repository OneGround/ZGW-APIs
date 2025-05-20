using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Authorization;

[AttributeUsage(AttributeTargets.Method)]
public abstract class BaseScopeAttribute : Attribute, IFilterFactory
{
    private readonly string _component;
    private readonly string[] _scopes;

    protected BaseScopeAttribute(string component, params string[] scopes)
    {
        _component = component;
        _scopes = scopes;
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var authorizationResolver = serviceProvider.GetRequiredService<IAuthorizationResolver>();
        var errorResponseBuilder = serviceProvider.GetRequiredService<IErrorResponseBuilder>();
        var logger = serviceProvider.GetService<ILogger<BaseScopeAttributeImpl>>();
        var organisationContextFactory = serviceProvider.GetRequiredService<IOrganisationContextFactory>();

        return new BaseScopeAttributeImpl(logger, authorizationResolver, errorResponseBuilder, organisationContextFactory, _component, _scopes);
    }
}

internal class BaseScopeAttributeImpl : IAsyncActionFilter
{
    private readonly string[] _scopes;
    private readonly IErrorResponseBuilder _errorResponseBuilder;
    private readonly string _component;
    private readonly IAuthorizationResolver _authorizationResolver;
    private readonly ILogger<BaseScopeAttributeImpl> _logger;
    private readonly IOrganisationContextFactory _organisationContextFactory;

    public BaseScopeAttributeImpl(
        ILogger<BaseScopeAttributeImpl> logger,
        IAuthorizationResolver authorizationResolver,
        IErrorResponseBuilder errorResponseBuilder,
        IOrganisationContextFactory organisationContextFactory,
        string component,
        params string[] scopes
    )
    {
        _logger = logger;
        _scopes = scopes;
        _errorResponseBuilder = errorResponseBuilder;
        _organisationContextFactory = organisationContextFactory;
        _component = component;
        _authorizationResolver = authorizationResolver;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var clientId = context.HttpContext.GetClientId();
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogError("Http context does not contain client id");
            context.Result = _errorResponseBuilder.Unauthorized();
            return;
        }

        var rsin = context.HttpContext.GetRsin();
        if (string.IsNullOrEmpty(rsin))
        {
            _logger.LogError("Http context does not contain rsin");
            context.Result = _errorResponseBuilder.Unauthorized();
            return;
        }

        _organisationContextFactory.Create(rsin);

        _logger.LogDebug("Resolving client authorizations / scopes for '{ClientId}' using {AuthorizationResolver}", clientId, _authorizationResolver);

        var application = await _authorizationResolver.ResolveAsync(clientId, _component, _scopes);
        if (application == null)
        {
            _logger.LogError("Failed to resolve client authorizations / scopes");
            context.Result = _errorResponseBuilder.Forbidden();
            return;
        }
        _logger.LogDebug("Resolved client authorizations");

        if (!application.HasAllAuthorizations && !application.Authorizations.Any())
        {
            // we can deny access if user does not have any scopes defined and is not a super user
            context.Result = _errorResponseBuilder.Forbidden();
            return;
        }

        application.Rsin = rsin;

        context.HttpContext.Items["authorizedApplicationLabel"] = application.Label;
        context.HttpContext.Items["authorizationContext"] = new AuthorizationContext(application, _scopes);

        await next.Invoke();
    }
}
