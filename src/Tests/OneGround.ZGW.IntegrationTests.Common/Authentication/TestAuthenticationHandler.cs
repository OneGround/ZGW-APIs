using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OneGround.ZGW.IntegrationTests.Common.Authentication;

/// <summary>
/// Turns an <c>Authorization: Test type=value;type=value</c> header (see <see cref="TestIdentity"/>) into an identity with those claims.
/// </summary>
public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!string.Equals(header.Scheme, TestAuthenticationDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = ParseClaims(header.Parameter).ToList();

        var identity = new ClaimsIdentity(claims, TestAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), TestAuthenticationDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static IEnumerable<Claim> ParseClaims(string parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
            yield break;

        foreach (var pair in parameter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
                continue;

            yield return new Claim(pair[..separator], pair[(separator + 1)..]);
        }
    }
}
