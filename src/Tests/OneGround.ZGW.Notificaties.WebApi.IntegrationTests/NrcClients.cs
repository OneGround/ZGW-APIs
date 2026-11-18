using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Web.Authorization;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Registers a new client id with the factory's stub Autorisaties API; every call returns a fresh id, so no cached authorization carries over.
/// </summary>
internal static class NrcClients
{
    /// <summary>
    /// Every scope a Notificaties API action can ask for.
    /// </summary>
    public static readonly string[] AllScopes = [AuthorizationScopes.Notificaties.Consume, AuthorizationScopes.Notificaties.Produce];

    /// <summary>
    /// A client whose only authorization is one <c>nrc</c> authorization with the given scopes.
    /// </summary>
    public static string Authorized(NotificatiesWebApplicationFactory factory, params string[] scopes)
    {
        return Grant(factory, "nrc", scopes);
    }

    /// <summary>
    /// A client authorized like <see cref="Authorized"/>, holding every scope in <see cref="AllScopes"/> except <paramref name="excluded"/>.
    /// </summary>
    public static string AuthorizedForAllScopesExcept(NotificatiesWebApplicationFactory factory, IEnumerable<string> excluded)
    {
        var scopes = AllScopes.Except(excluded).ToArray();
        if (scopes.Length == 0)
            throw new ArgumentException("Excluding every scope leaves a client without authorizations.", nameof(excluded));

        return Authorized(factory, scopes);
    }

    /// <summary>
    /// A client holding both Notificaties scopes, but on another component's authorization.
    /// </summary>
    public static string AuthorizedOnAnotherComponent(NotificatiesWebApplicationFactory factory)
    {
        return Grant(factory, "zrc", AllScopes);
    }

    public static string WithAllAuthorizations(NotificatiesWebApplicationFactory factory)
    {
        var clientId = NewClientId();
        factory.AutorisatiesApi.GrantAllAuthorizations(clientId);

        return clientId;
    }

    public static string WithoutAuthorizations(NotificatiesWebApplicationFactory factory)
    {
        var clientId = NewClientId();
        factory.AutorisatiesApi.GrantNoAuthorizations(clientId);

        return clientId;
    }

    public static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";

    private static string Grant(NotificatiesWebApplicationFactory factory, string component, string[] scopes)
    {
        var clientId = NewClientId();

        factory.AutorisatiesApi.Grant(clientId, new AutorisatieResponseDto { Component = component, Scopes = scopes });

        return clientId;
    }
}
