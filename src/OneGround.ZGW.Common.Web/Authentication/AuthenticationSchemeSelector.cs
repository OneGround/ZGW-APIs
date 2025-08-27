using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Common.Web.Authentication;

public static class AuthenticationSchemeSelector
{
    public static string SelectAuthenticationScheme(HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(AuthenticationSchemeSelector));
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return ZgwAuthenticationConstants.OAuth2AuthenticationScheme;
        }

        const string bearerPrefix = "Bearer ";
        try
        {
            var token = authHeader[bearerPrefix.Length..];
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var alg = jwt.Header?.Alg;
            if (!string.IsNullOrEmpty(alg) && alg.Equals("HS256", StringComparison.OrdinalIgnoreCase))
            {
                return ZgwAuthenticationConstants.ZgwTokenIntrospectionAuthenticationScheme;
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Error parsing JWT token in authentication scheme selector");
        }

        return ZgwAuthenticationConstants.OAuth2AuthenticationScheme;
    }
}
