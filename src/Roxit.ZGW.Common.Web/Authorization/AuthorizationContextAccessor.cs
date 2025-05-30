using Microsoft.AspNetCore.Http;

namespace Roxit.ZGW.Common.Web.Authorization;

public interface IAuthorizationContextAccessor
{
    public AuthorizationContext AuthorizationContext { get; }
}

public class AuthorizationContextAccessor : IAuthorizationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthorizationContext AuthorizationContext
    {
        get { return _httpContextAccessor.HttpContext.Items["authorizationContext"] as AuthorizationContext; }
    }
}
