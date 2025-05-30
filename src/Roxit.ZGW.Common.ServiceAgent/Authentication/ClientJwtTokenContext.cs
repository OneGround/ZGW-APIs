using System;
using Microsoft.AspNetCore.Http;

namespace Roxit.ZGW.Common.ServiceAgent.Authentication;

public interface IClientJwtTokenContext
{
    string Token { get; }
}

public class ClientJwtTokenContext : IClientJwtTokenContext
{
    const string BearerPrefix = "Bearer";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientJwtTokenContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Token
    {
        get
        {
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                if (authHeader.ToString().StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.ToString().Substring(BearerPrefix.Length).Trim();

                    return token;
                }
            }
            return null;
        }
    }
}
