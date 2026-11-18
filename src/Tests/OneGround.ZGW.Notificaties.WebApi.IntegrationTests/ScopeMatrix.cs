using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Web.Controllers.v1;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// The resource a request on a <see cref="ScopeMatrixRow"/> needs to exist; the route's <c>{id}</c> segment names it.
/// </summary>
internal enum NrcResource
{
    None,
    Kanaal,
    Abonnement,
}

/// <summary>
/// One <c>[Scope]</c>-carrying Notificaties controller action: its route and the scopes it accepts.
/// </summary>
internal sealed record ScopeMatrixRow(Type Controller, string Action, HttpMethod Method, string Route, string[] Scopes, NrcResource Needs)
{
    public string Name => NameOf(Controller, Action);

    public override string ToString() => Name;

    public static string NameOf(Type controller, string action) => $"{controller.Name}.{action}";
}

/// <summary>
/// The hand-written scope table for every <c>[Scope]</c>-carrying Notificaties action, held to the attributes by <see cref="ScopeMatrixCompletenessTests"/>.
/// </summary>
internal static class ScopeMatrix
{
    private const string Consume = AuthorizationScopes.Notificaties.Consume;
    private const string Produce = AuthorizationScopes.Notificaties.Produce;

    public static readonly ScopeMatrixRow[] Rows =
    [
        Row<KanaalController>(nameof(KanaalController.GetAllAsync), HttpMethod.Get, ApiRoutes.Kanaal.GetAll, NrcResource.None, Produce, Consume),
        Row<KanaalController>(nameof(KanaalController.GetAsync), HttpMethod.Get, ApiRoutes.Kanaal.Get, NrcResource.Kanaal, Produce, Consume),
        Row<KanaalController>(nameof(KanaalController.CreateAsync), HttpMethod.Post, ApiRoutes.Kanaal.Create, NrcResource.None, Produce),
        Row<AbonnementController>(
            nameof(AbonnementController.GetAllAsync),
            HttpMethod.Get,
            ApiRoutes.Abonnement.GetAll,
            NrcResource.None,
            Consume,
            Produce
        ),
        Row<AbonnementController>(
            nameof(AbonnementController.GetAsync),
            HttpMethod.Get,
            ApiRoutes.Abonnement.Get,
            NrcResource.Abonnement,
            Consume,
            Produce
        ),
        Row<AbonnementController>(nameof(AbonnementController.CreateAsync), HttpMethod.Post, ApiRoutes.Abonnement.Create, NrcResource.None, Consume),
        Row<AbonnementController>(
            nameof(AbonnementController.UpdateAsync),
            HttpMethod.Put,
            ApiRoutes.Abonnement.Update,
            NrcResource.Abonnement,
            Consume
        ),
        Row<AbonnementController>(
            nameof(AbonnementController.PartialUpdateAsync),
            HttpMethod.Patch,
            ApiRoutes.Abonnement.Update,
            NrcResource.Abonnement,
            Consume
        ),
        Row<AbonnementController>(
            nameof(AbonnementController.DeleteAsync),
            HttpMethod.Delete,
            ApiRoutes.Abonnement.Delete,
            NrcResource.Abonnement,
            Consume
        ),
        Row<NotificatiesController>(
            nameof(NotificatiesController.NotificeerAsync),
            HttpMethod.Post,
            ApiRoutes.Notificaties.Create,
            NrcResource.Kanaal,
            Produce
        ),
    ];

    public static ScopeMatrixRow Named(string name) => Rows.Single(r => r.Name == name);

    /// <summary>
    /// The rows whose action accepts only some of <see cref="NrcClients.AllScopes"/>, so a client holding the rest is denied.
    /// </summary>
    public static IEnumerable<ScopeMatrixRow> RowsDeniedBySomeScope => Rows.Where(r => NrcClients.AllScopes.Except(r.Scopes).Any());

    private static ScopeMatrixRow Row<TController>(string action, HttpMethod method, string route, NrcResource needs, params string[] scopes)
    {
        return new ScopeMatrixRow(typeof(TController), action, method, route, scopes, needs);
    }
}
