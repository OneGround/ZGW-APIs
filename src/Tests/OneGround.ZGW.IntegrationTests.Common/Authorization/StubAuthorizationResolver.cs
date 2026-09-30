using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.IntegrationTests.Common.Authorization;

/// <summary>
/// An <see cref="IAuthorizationResolver"/> a test configures per client id, in place of the resolver that asks the
/// Autorisaties API. A client id nobody configured resolves to <c>null</c>, as an unknown application does.
/// Everything that uses the resolved application (the scope filters and the handlers) runs unchanged.
/// </summary>
public sealed class StubAuthorizationResolver : IAuthorizationResolver
{
    private readonly ConcurrentDictionary<string, Func<AuthorizedApplication>> _applications = new();

    private readonly ConcurrentQueue<AuthorizationResolution> _resolutions = new();

    /// <summary>
    /// Every call the API made to this resolver, in order.
    /// </summary>
    public AuthorizationResolution[] Resolutions => _resolutions.ToArray();

    /// <summary>
    /// The client resolves to <c>null</c> (no application found).
    /// </summary>
    public void ResolveNothingFor(string clientId)
    {
        _applications.TryRemove(clientId, out _);
    }

    /// <summary>
    /// The client resolves to an application with no authorizations and <see cref="AuthorizedApplication.HasAllAuthorizations"/> false.
    /// </summary>
    public void GrantNoAuthorizations(string clientId)
    {
        Set(clientId, hasAllAuthorizations: false, []);
    }

    /// <summary>
    /// The client resolves to an application with <see cref="AuthorizedApplication.HasAllAuthorizations"/> true (a super user).
    /// </summary>
    public void GrantAllAuthorizations(string clientId)
    {
        Set(clientId, hasAllAuthorizations: true, []);
    }

    /// <summary>
    /// The client resolves to an application with exactly these permissions.
    /// </summary>
    public void GrantPermissions(string clientId, params AuthorizationPermission[] permissions)
    {
        Set(clientId, hasAllAuthorizations: false, permissions);
    }

    public Task<AuthorizedApplication> ResolveAsync(string clientId, string component, string[] scopes)
    {
        _resolutions.Enqueue(new AuthorizationResolution(clientId, component, scopes));

        var application = _applications.TryGetValue(clientId, out var create) ? create() : null;

        return Task.FromResult(application);
    }

    private void Set(string clientId, bool hasAllAuthorizations, AuthorizationPermission[] permissions)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        // A new instance per call: the scope filter writes the request's rsin into the application it resolved
        _applications[clientId] = () =>
            new AuthorizedApplication
            {
                Label = clientId,
                HasAllAuthorizations = hasAllAuthorizations,
                Authorizations = permissions.ToList(),
            };
    }
}

public sealed record AuthorizationResolution(string ClientId, string Component, string[] Scopes);
