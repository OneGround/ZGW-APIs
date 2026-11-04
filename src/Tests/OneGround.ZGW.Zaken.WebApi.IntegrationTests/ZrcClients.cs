using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Registers a new client id with the factory's stub Autorisaties API; every call returns a fresh id, so no cached authorization carries over.
/// </summary>
internal static class ZrcClients
{
    /// <summary>
    /// Every scope a Zaken API action can ask for: the Zaken scopes and the audittrail scope.
    /// </summary>
    public static readonly string[] AllScopes =
    [
        AuthorizationScopes.Zaken.Read,
        AuthorizationScopes.Zaken.Create,
        AuthorizationScopes.Zaken.Update,
        AuthorizationScopes.Zaken.ForcedUpdate,
        AuthorizationScopes.Zaken.Delete,
        AuthorizationScopes.Zaken.Reopen,
        AuthorizationScopes.Zaken.Statuses.Add,
        AuthorizationScopes.AuditTrails.Read,
    ];

    /// <summary>
    /// A client whose only authorization is one <c>zrc</c> authorization with the given scopes, zaaktype and maximum vertrouwelijkheidaanduiding.
    /// </summary>
    public static string Authorized(ZakenWebApplicationFactory factory, string zaakType, VertrouwelijkheidAanduiding maximum, params string[] scopes)
    {
        var clientId = NewClientId();

        factory.AutorisatiesApi.Grant(
            clientId,
            new AutorisatieResponseDto
            {
                Component = "zrc",
                Scopes = scopes,
                ZaakType = zaakType,
                MaxVertrouwelijkheidaanduiding = maximum.ToString(),
            }
        );

        return clientId;
    }

    /// <summary>
    /// A client authorized like <see cref="Authorized"/>, holding every scope in <see cref="AllScopes"/> except <paramref name="excluded"/>.
    /// </summary>
    public static string AuthorizedForAllScopesExcept(
        ZakenWebApplicationFactory factory,
        string zaakType,
        VertrouwelijkheidAanduiding maximum,
        IEnumerable<string> excluded
    )
    {
        var scopes = AllScopes.Except(excluded).ToArray();
        if (scopes.Length == 0)
            throw new ArgumentException("Excluding every scope leaves a client without authorizations.", nameof(excluded));

        return Authorized(factory, zaakType, maximum, scopes);
    }

    public static string WithAllAuthorizations(ZakenWebApplicationFactory factory)
    {
        var clientId = NewClientId();
        factory.AutorisatiesApi.GrantAllAuthorizations(clientId);

        return clientId;
    }

    public static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
