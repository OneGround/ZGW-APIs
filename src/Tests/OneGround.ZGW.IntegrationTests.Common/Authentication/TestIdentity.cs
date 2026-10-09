using System.Collections.Generic;
using System.Net.Http.Headers;
using OneGround.ZGW.Common.Authentication;

namespace OneGround.ZGW.IntegrationTests.Common.Authentication;

/// <summary>
/// Builds the Authorization header <see cref="TestAuthenticationHandler"/> turns into an identity.
/// </summary>
public static class TestIdentity
{
    /// <summary>
    /// An identity with the given <c>client_id</c> and <c>rsin</c> claims; a <c>null</c> value leaves that claim out.
    /// </summary>
    public static AuthenticationHeaderValue Create(string clientId, string rsin)
    {
        var claims = new List<string>();

        if (clientId != null)
            claims.Add($"{CustomClaimTypes.ClientId}={clientId}");

        if (rsin != null)
            claims.Add($"{CustomClaimTypes.Rsin}={rsin}");

        return new AuthenticationHeaderValue(TestAuthenticationDefaults.AuthenticationScheme, string.Join(";", claims));
    }
}
