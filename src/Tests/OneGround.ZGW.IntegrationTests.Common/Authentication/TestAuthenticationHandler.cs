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
using OneGround.ZGW.Common.Authentication;

namespace OneGround.ZGW.IntegrationTests.Common.Authentication;

/// <summary>
/// Authenticates a request from a <c>Test</c> Authorization header instead of a real access token.
/// <para>
/// A request without an Authorization header (or with a header for another scheme) gets no identity, exactly like an
/// anonymous request. <c>Authorization: Test client_id=my-client;rsin=123456789</c> gets an identity with those claims;
/// leave a claim out to test what the API does without it. Use <see cref="TestIdentity"/> to build the header.
/// </para>
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

            var type = pair[..separator];
            var value = pair[(separator + 1)..];

            if (type == CustomClaimTypes.ClientId || type == CustomClaimTypes.Rsin)
            {
                yield return new Claim(type, value);
            }
        }
    }
}
