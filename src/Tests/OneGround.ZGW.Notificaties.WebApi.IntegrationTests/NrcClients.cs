using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.IntegrationTests.Common.Authorization;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Registers a new client id with the factory's stub Autorisaties API; every call returns a fresh id, so no cached authorization carries over.
/// </summary>
internal static class NrcClients
{
    /// <summary>
    /// A client whose only authorization is one <c>nrc</c> authorization with the given scopes.
    /// </summary>
    public static string Authorized(NotificatiesWebApplicationFactory factory, params string[] scopes)
    {
        return Grant(factory, "nrc", scopes);
    }

    /// <summary>
    /// A client authorized like <see cref="Authorized"/>, holding every scope in <see cref="ScopeMatrix.AllScopes"/> except <paramref name="excluded"/>.
    /// </summary>
    public static string AuthorizedForAllScopesExcept(NotificatiesWebApplicationFactory factory, IEnumerable<string> excluded)
    {
        var scopes = ScopeMatrix.AllScopes.Except(excluded).ToArray();
        if (scopes.Length == 0)
            throw new ArgumentException("Excluding every scope leaves a client without authorizations.", nameof(excluded));

        return Authorized(factory, scopes);
    }

    /// <summary>
    /// A client holding both Notificaties scopes, but on another component's authorization.
    /// </summary>
    public static string AuthorizedOnAnotherComponent(NotificatiesWebApplicationFactory factory)
    {
        return Grant(factory, "zrc", ScopeMatrix.AllScopes);
    }

    public static string WithAllAuthorizations(NotificatiesWebApplicationFactory factory) => factory.AutorisatiesApi.WithAllAuthorizations();

    public static string WithoutAuthorizations(NotificatiesWebApplicationFactory factory) => factory.AutorisatiesApi.WithoutAuthorizations();

    public static string NewClientId() => StubAutorisatiesApi.NewClientId();

    private static string Grant(NotificatiesWebApplicationFactory factory, string component, string[] scopes)
    {
        var clientId = StubAutorisatiesApi.NewClientId();

        factory.AutorisatiesApi.Grant(clientId, new AutorisatieResponseDto { Component = component, Scopes = scopes });

        return clientId;
    }
}
