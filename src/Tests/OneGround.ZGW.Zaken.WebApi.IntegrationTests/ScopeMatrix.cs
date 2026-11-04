using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Web.Controllers;
using V1 = OneGround.ZGW.Zaken.Web.Controllers.v1;
using V1_2 = OneGround.ZGW.Zaken.Web.Controllers.v1._2;
using V1_2Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._2.ApiRoutes;
using V1_5 = OneGround.ZGW.Zaken.Web.Controllers.v1._5;
using V1_5Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._5.ApiRoutes;
using V1_7 = OneGround.ZGW.Zaken.Web.Controllers.v1._7;
using V1_7Routes = OneGround.ZGW.Zaken.Web.Contracts.v1._7.ApiRoutes;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// One <c>[Scope]</c>-carrying Zaken controller action: its route, one API version it serves, the scopes it accepts, and the resource its
/// request needs seeded under the zaak.
/// </summary>
internal sealed record ScopeMatrixRow(
    Type Controller,
    string Action,
    HttpMethod Method,
    string Route,
    string ApiVersion,
    string[] Scopes,
    ZaakResource Seeds
)
{
    public string Name => NameOf(Controller, Action);

    public MethodInfo ActionMethod => Controller.GetMethod(Action, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

    // A controller declares the versions it serves either on the class or on each of its actions.
    public string[] ServedApiVersions => VersionsDeclaredOn(Controller).Concat(VersionsDeclaredOn(ActionMethod)).ToArray();

    public override string ToString() => Name;

    public static string NameOf(Type controller, string action) =>
        $"{controller.FullName!["OneGround.ZGW.Zaken.Web.Controllers.".Length..]}.{action}";

    private static IEnumerable<string> VersionsDeclaredOn(MemberInfo member)
    {
        return member
            .GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(ZgwApiVersionAttribute))
            .Select(a => (string)a.ConstructorArguments[0].Value);
    }
}

/// <summary>
/// The hand-written scope table for every <c>[Scope]</c>-carrying Zaken action, held to the attributes by <see cref="ScopeMatrixCompletenessTests"/>.
/// </summary>
internal static class ScopeMatrix
{
    private const string Read = AuthorizationScopes.Zaken.Read;
    private const string Create = AuthorizationScopes.Zaken.Create;
    private const string Update = AuthorizationScopes.Zaken.Update;
    private const string ForcedUpdate = AuthorizationScopes.Zaken.ForcedUpdate;
    private const string Delete = AuthorizationScopes.Zaken.Delete;
    private const string Reopen = AuthorizationScopes.Zaken.Reopen;
    private const string AddStatus = AuthorizationScopes.Zaken.Statuses.Add;
    private const string AuditTrailRead = AuthorizationScopes.AuditTrails.Read;

    private const string V1_0 = Api.LatestVersion_1_0;
    private const string V1_2_0 = Api.LatestVersion_1_2;
    private const string V1_5_1 = Api.LatestVersion_1_5;
    private const string V1_7_0 = Api.LatestVersion_1_7;

    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Head = HttpMethod.Head;
    private static readonly HttpMethod Post = HttpMethod.Post;
    private static readonly HttpMethod Put = HttpMethod.Put;
    private static readonly HttpMethod Patch = HttpMethod.Patch;
    private static readonly HttpMethod DeleteMethod = HttpMethod.Delete;

    public static readonly IReadOnlyList<ScopeMatrixRow> Rows =
    [
        // Controllers/v1/KlantContactenController.cs
        Row<V1.KlantContactenController>(
            nameof(V1.KlantContactenController.GetAllAsync),
            Get,
            V1Routes.KlantContacten.GetAll,
            V1_0,
            ZaakResource.KlantContact,
            Read
        ),
        Row<V1.KlantContactenController>(
            nameof(V1.KlantContactenController.GetAsync),
            Get,
            V1Routes.KlantContacten.Get,
            V1_0,
            ZaakResource.KlantContact,
            Read
        ),
        Row<V1.KlantContactenController>(
            nameof(V1.KlantContactenController.AddAsync),
            Post,
            V1Routes.KlantContacten.Create,
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/ZaakInformatieObjectenController.cs
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.GetAllAsync),
            Get,
            V1Routes.ZaakInformatieObjecten.GetAll,
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.GetAsync),
            Get,
            V1Routes.ZaakInformatieObjecten.Get,
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.AddAsync),
            Post,
            V1Routes.ZaakInformatieObjecten.Create,
            V1_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.UpdateAsync),
            Put,
            V1Routes.ZaakInformatieObjecten.Update,
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.PartialUpdateAsync),
            Patch,
            V1Routes.ZaakInformatieObjecten.Update,
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            nameof(V1.ZaakInformatieObjectenController.DeleteAsync),
            DeleteMethod,
            V1Routes.ZaakInformatieObjecten.Delete,
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/ZaakObjectenController.cs
        Row<V1.ZaakObjectenController>(
            nameof(V1.ZaakObjectenController.GetAllAsync),
            Get,
            V1Routes.ZaakObjecten.GetAll,
            V1_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1.ZaakObjectenController>(
            nameof(V1.ZaakObjectenController.GetAsync),
            Get,
            V1Routes.ZaakObjecten.Get,
            V1_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1.ZaakObjectenController>(
            nameof(V1.ZaakObjectenController.AddAsync),
            Post,
            V1Routes.ZaakObjecten.Create,
            V1_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/ZaakResultatenController.cs
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.GetAllAsync),
            Get,
            V1Routes.ZaakResultaten.GetAll,
            V1_0,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.GetAsync),
            Get,
            V1Routes.ZaakResultaten.Get,
            V1_0,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.AddAsync),
            Post,
            V1Routes.ZaakResultaten.Create,
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.UpdateAsync),
            Put,
            V1Routes.ZaakResultaten.Update,
            V1_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.PartialUpdateAsync),
            Patch,
            V1Routes.ZaakResultaten.Update,
            V1_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakResultatenController>(
            nameof(V1.ZaakResultatenController.DeleteAsync),
            DeleteMethod,
            V1Routes.ZaakResultaten.Delete,
            V1_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/ZaakRollenController.cs
        Row<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.GetAllAsync), Get, V1Routes.ZaakRollen.GetAll, V1_0, ZaakResource.Rol, Read),
        Row<V1.ZaakRollenController>(nameof(V1.ZaakRollenController.GetAsync), Get, V1Routes.ZaakRollen.Get, V1_0, ZaakResource.Rol, Read),
        Row<V1.ZaakRollenController>(
            nameof(V1.ZaakRollenController.AddAsync),
            Post,
            V1Routes.ZaakRollen.Create,
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakRollenController>(
            nameof(V1.ZaakRollenController.DeleteAsync),
            DeleteMethod,
            V1Routes.ZaakRollen.Delete,
            V1_0,
            ZaakResource.Rol,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/ZaakStatussenController.cs
        Row<V1.ZaakStatussenController>(
            nameof(V1.ZaakStatussenController.GetAllAsync),
            Get,
            V1Routes.ZaakStatussen.GetAll,
            V1_0,
            ZaakResource.Status,
            Read
        ),
        Row<V1.ZaakStatussenController>(
            nameof(V1.ZaakStatussenController.GetAsync),
            Get,
            V1Routes.ZaakStatussen.Get,
            V1_0,
            ZaakResource.Status,
            Read
        ),
        Row<V1.ZaakStatussenController>(
            nameof(V1.ZaakStatussenController.AddAsync),
            Post,
            V1Routes.ZaakStatussen.Create,
            V1_0,
            ZaakResource.None,
            Create,
            AddStatus,
            Reopen
        ),
        // Controllers/v1/ZakenController.cs
        Row<V1.ZakenController>(nameof(V1.ZakenController.GetAllAsync), Get, V1Routes.Zaken.GetAll, V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>(nameof(V1.ZakenController.GetAsync), Get, V1Routes.Zaken.Get, V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>(nameof(V1.ZakenController.SearchAsync), Post, V1Routes.Zaken.Search, V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>(nameof(V1.ZakenController.AddAsync), Post, V1Routes.Zaken.Create, V1_0, ZaakResource.None, Create),
        Row<V1.ZakenController>(nameof(V1.ZakenController.UpdateAsync), Put, V1Routes.Zaken.Update, V1_0, ZaakResource.None, Update, ForcedUpdate),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.PartialUpdateAsync),
            Patch,
            V1Routes.Zaken.Update,
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZakenController>(nameof(V1.ZakenController.DeleteAsync), DeleteMethod, V1Routes.Zaken.Delete, V1_0, ZaakResource.None, Delete),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetAllZaakAuditTrailRegelsAsync),
            Get,
            V1Routes.ZaakAudittrail.GetAll,
            V1_0,
            ZaakResource.AuditTrailRegel,
            AuditTrailRead
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetZaakAuditTrailRegelAsync),
            Get,
            V1Routes.ZaakAudittrail.Get,
            V1_0,
            ZaakResource.AuditTrailRegel,
            AuditTrailRead
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetAllZaakBesluitenAsync),
            Get,
            V1Routes.ZaakBesluiten.GetAll,
            V1_0,
            ZaakResource.Besluit,
            Read
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.AddZaakBesluitenAsync),
            Post,
            V1Routes.ZaakBesluiten.Create,
            V1_0,
            ZaakResource.None,
            Update
        ),
        Row<V1.ZakenController>(nameof(V1.ZakenController.GetZaakBesluitAsync), Get, V1Routes.ZaakBesluiten.Get, V1_0, ZaakResource.Besluit, Read),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.DeleteZaakBesluitAsync),
            DeleteMethod,
            V1Routes.ZaakBesluiten.Delete,
            V1_0,
            ZaakResource.Besluit,
            Update
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetAllZaakEigenschappenAsync),
            Get,
            V1Routes.ZaakEigenschappen.GetAll,
            V1_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.AddZaakEigenschapAsync),
            Post,
            V1Routes.ZaakEigenschappen.Create,
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetZaakEigenschapAsync),
            Get,
            V1Routes.ZaakEigenschappen.Get,
            V1_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.GetZaakEigenschapV1_2Async),
            Get,
            V1Routes.ZaakEigenschappen.Get,
            V1_2_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1.ZakenController>(
            nameof(V1.ZakenController.HeadZaakEigenschapV1_2Async),
            Head,
            V1Routes.ZaakEigenschappen.Get,
            V1_2_0,
            ZaakResource.Eigenschap,
            Read
        ),
        // Controllers/v1/2/ZaakObjectenController.cs
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.GetAllAsync),
            Get,
            V1_2Routes.ZaakObjecten.GetAll,
            V1_2_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.GetAsync),
            Get,
            V1_2Routes.ZaakObjecten.Get,
            V1_2_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.AddAsync),
            Post,
            V1_2Routes.ZaakObjecten.Create,
            V1_2_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.UpdateAsync),
            Put,
            V1_2Routes.ZaakObjecten.Update,
            V1_2_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.PartialUpdateAsync),
            Patch,
            V1_2Routes.ZaakObjecten.Update,
            V1_2_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZaakObjectenController>(
            nameof(V1_2.ZaakObjectenController.DeleteAsync),
            DeleteMethod,
            V1_2Routes.ZaakObjecten.Delete,
            V1_2_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/2/ZakenController.cs
        Row<V1_2.ZakenController>(
            nameof(V1_2.ZakenController.UpdateAsync),
            Put,
            V1_2Routes.ZaakEigenschappen.Update,
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZakenController>(
            nameof(V1_2.ZakenController.PartialUpdateAsync),
            Patch,
            V1_2Routes.ZaakEigenschappen.Update,
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZakenController>(
            nameof(V1_2.ZakenController.DeleteAsync),
            DeleteMethod,
            V1_2Routes.ZaakEigenschappen.Delete,
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZaakContactMomentenController.cs
        Row<V1_5.ZaakContactmomentenController>(
            nameof(V1_5.ZaakContactmomentenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakContactmomenten.GetAll,
            V1_5_1,
            ZaakResource.Contactmoment,
            Read
        ),
        Row<V1_5.ZaakContactmomentenController>(
            nameof(V1_5.ZaakContactmomentenController.GetAsync),
            Get,
            V1_5Routes.ZaakContactmomenten.Get,
            V1_5_1,
            ZaakResource.Contactmoment,
            Read
        ),
        Row<V1_5.ZaakContactmomentenController>(
            nameof(V1_5.ZaakContactmomentenController.AddAsync),
            Post,
            V1_5Routes.ZaakContactmomenten.Create,
            V1_5_1,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakContactmomentenController>(
            nameof(V1_5.ZaakContactmomentenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakContactmomenten.Delete,
            V1_5_1,
            ZaakResource.Contactmoment,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZaakInformatieObjectenController.cs
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakInformatieObjecten.GetAll,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.GetAsync),
            Get,
            V1_5Routes.ZaakInformatieObjecten.Get,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.HeadAsync),
            Head,
            V1_5Routes.ZaakInformatieObjecten.Get,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.AddAsync),
            Post,
            V1_5Routes.ZaakInformatieObjecten.Create,
            V1_5_1,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.UpdateAsync),
            Put,
            V1_5Routes.ZaakInformatieObjecten.Update,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.ZaakInformatieObjecten.Update,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            nameof(V1_5.ZaakInformatieObjectenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakInformatieObjecten.Delete,
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/5/ZaakObjectenController.cs
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakObjecten.GetAll,
            V1_5_1,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.GetAsync),
            Get,
            V1_5Routes.ZaakObjecten.Get,
            V1_5_1,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.HeadAsync),
            Head,
            V1_5Routes.ZaakObjecten.Get,
            V1_5_1,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.AddAsync),
            Post,
            V1_5Routes.ZaakObjecten.Create,
            V1_5_1,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.UpdateAsync),
            Put,
            V1_5Routes.ZaakObjecten.Update,
            V1_5_1,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.ZaakObjecten.Update,
            V1_5_1,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakObjectenController>(
            nameof(V1_5.ZaakObjectenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakObjecten.Delete,
            V1_5_1,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/5/ZaakResultatenController.cs
        Row<V1_5.ZaakResultatenController>(
            nameof(V1_5.ZaakResultatenController.GetAsync),
            Get,
            V1_5Routes.ZaakResultaten.Get,
            V1_5_1,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1_5.ZaakResultatenController>(
            nameof(V1_5.ZaakResultatenController.HeadAsync),
            Head,
            V1_5Routes.ZaakResultaten.Get,
            V1_5_1,
            ZaakResource.Resultaat,
            Read
        ),
        // Controllers/v1/5/ZaakRollenController.cs
        Row<V1_5.ZaakRollenController>(
            nameof(V1_5.ZaakRollenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakRollen.GetAll,
            V1_5_1,
            ZaakResource.Rol,
            Read
        ),
        Row<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.GetAsync), Get, V1_5Routes.ZaakRollen.Get, V1_5_1, ZaakResource.Rol, Read),
        Row<V1_5.ZaakRollenController>(nameof(V1_5.ZaakRollenController.HeadAsync), Head, V1_5Routes.ZaakRollen.Get, V1_5_1, ZaakResource.Rol, Read),
        Row<V1_5.ZaakRollenController>(
            nameof(V1_5.ZaakRollenController.AddAsync),
            Post,
            V1_5Routes.ZaakRollen.Create,
            V1_5_1,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakRollenController>(
            nameof(V1_5.ZaakRollenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakRollen.Delete,
            V1_5_1,
            ZaakResource.Rol,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZaakStatussenController.cs
        Row<V1_5.ZaakStatussenController>(
            nameof(V1_5.ZaakStatussenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakStatussen.GetAll,
            V1_5_1,
            ZaakResource.Status,
            Read
        ),
        Row<V1_5.ZaakStatussenController>(
            nameof(V1_5.ZaakStatussenController.GetAsync),
            Get,
            V1_5Routes.ZaakStatussen.Get,
            V1_5_1,
            ZaakResource.Status,
            Read
        ),
        Row<V1_5.ZaakStatussenController>(
            nameof(V1_5.ZaakStatussenController.HeadAsync),
            Head,
            V1_5Routes.ZaakStatussen.Get,
            V1_5_1,
            ZaakResource.Status,
            Read
        ),
        Row<V1_5.ZaakStatussenController>(
            nameof(V1_5.ZaakStatussenController.AddAsync),
            Post,
            V1_5Routes.ZaakStatussen.Create,
            V1_5_1,
            ZaakResource.None,
            Create,
            AddStatus,
            Reopen
        ),
        // Controllers/v1/5/ZaakVerzoekenController.cs
        Row<V1_5.ZaakVerzoekenController>(
            nameof(V1_5.ZaakVerzoekenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakVerzoeken.GetAll,
            V1_5_1,
            ZaakResource.Verzoek,
            Read
        ),
        Row<V1_5.ZaakVerzoekenController>(
            nameof(V1_5.ZaakVerzoekenController.GetAsync),
            Get,
            V1_5Routes.ZaakVerzoeken.Get,
            V1_5_1,
            ZaakResource.Verzoek,
            Read
        ),
        Row<V1_5.ZaakVerzoekenController>(
            nameof(V1_5.ZaakVerzoekenController.AddAsync),
            Post,
            V1_5Routes.ZaakVerzoeken.Create,
            V1_5_1,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakVerzoekenController>(
            nameof(V1_5.ZaakVerzoekenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakVerzoeken.Delete,
            V1_5_1,
            ZaakResource.Verzoek,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZakenController.cs
        Row<V1_5.ZakenController>(nameof(V1_5.ZakenController.GetAllAsync), Get, V1_5Routes.Zaken.GetAll, V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>(nameof(V1_5.ZakenController.SearchAsync), Post, V1_5Routes.Zaken.Search, V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>(nameof(V1_5.ZakenController.AddAsync), Post, V1_5Routes.Zaken.Create, V1_5_1, ZaakResource.None, Create),
        Row<V1_5.ZakenController>(nameof(V1_5.ZakenController.GetAsync), Get, V1_5Routes.Zaken.Get, V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>(nameof(V1_5.ZakenController.HeadAsync), Head, V1_5Routes.Zaken.Get, V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>(
            nameof(V1_5.ZakenController.UpdateAsync),
            Put,
            V1_5Routes.Zaken.Update,
            V1_5_1,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZakenController>(
            nameof(V1_5.ZakenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.Zaken.Update,
            V1_5_1,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZakenController>(
            nameof(V1_5.ZakenController.GetZaakEigenschapAsync),
            Get,
            V1_5Routes.ZaakEigenschappen.Get,
            V1_5_1,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1_5.ZakenController>(
            nameof(V1_5.ZakenController.HeadZaakEigenschapAsync),
            Head,
            V1_5Routes.ZaakEigenschappen.Get,
            V1_5_1,
            ZaakResource.Eigenschap,
            Read
        ),
        // Controllers/v1/7/ZaakContactmomentenController.cs
        Row<V1_7.ZaakContactmomentenController>(
            nameof(V1_7.ZaakContactmomentenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakContactmomenten.GetAll,
            V1_7_0,
            ZaakResource.Contactmoment,
            Read
        ),
        Row<V1_7.ZaakContactmomentenController>(
            nameof(V1_7.ZaakContactmomentenController.GetAsync),
            Get,
            V1_5Routes.ZaakContactmomenten.Get,
            V1_7_0,
            ZaakResource.Contactmoment,
            Read
        ),
        Row<V1_7.ZaakContactmomentenController>(
            nameof(V1_7.ZaakContactmomentenController.AddAsync),
            Post,
            V1_5Routes.ZaakContactmomenten.Create,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakContactmomentenController>(
            nameof(V1_7.ZaakContactmomentenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakContactmomenten.Delete,
            V1_7_0,
            ZaakResource.Contactmoment,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/7/ZaakInformatieObjectenController.cs
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakInformatieObjecten.GetAll,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.GetAsync),
            Get,
            V1_5Routes.ZaakInformatieObjecten.Get,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.HeadAsync),
            Head,
            V1_5Routes.ZaakInformatieObjecten.Get,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.AddAsync),
            Post,
            V1_5Routes.ZaakInformatieObjecten.Create,
            V1_7_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.UpdateAsync),
            Put,
            V1_5Routes.ZaakInformatieObjecten.Update,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.ZaakInformatieObjecten.Update,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakInformatieObjectenController>(
            nameof(V1_7.ZaakInformatieObjectenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakInformatieObjecten.Delete,
            V1_7_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/7/ZaaknummerReserverenController.cs
        Row<V1_7.ZaaknummerReserverenController>(
            nameof(V1_7.ZaaknummerReserverenController.ReserveAsync),
            Post,
            V1_7Routes.ZaaknummerReserveren.Create,
            V1_7_0,
            ZaakResource.None,
            Create
        ),
        // Controllers/v1/7/ZaakNotitiesController.cs
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.GetAllAsync),
            Get,
            V1_7Routes.ZaakNotities.GetAll,
            V1_7_0,
            ZaakResource.Notitie,
            Read
        ),
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.GetAsync),
            Get,
            V1_7Routes.ZaakNotities.Get,
            V1_7_0,
            ZaakResource.Notitie,
            Read
        ),
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.AddAsync),
            Post,
            V1_7Routes.ZaakNotities.Create,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.UpdateAsync),
            Put,
            V1_7Routes.ZaakNotities.Update,
            V1_7_0,
            ZaakResource.Notitie,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.PartialUpdateAsync),
            Patch,
            V1_7Routes.ZaakNotities.Update,
            V1_7_0,
            ZaakResource.Notitie,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakNotitiesController>(
            nameof(V1_7.ZaakNotitiesController.DeleteAsync),
            DeleteMethod,
            V1_7Routes.ZaakNotities.Delete,
            V1_7_0,
            ZaakResource.Notitie,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/7/ZaakObjectenController.cs
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakObjecten.GetAll,
            V1_7_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.GetAsync),
            Get,
            V1_5Routes.ZaakObjecten.Get,
            V1_7_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.HeadAsync),
            Head,
            V1_5Routes.ZaakObjecten.Get,
            V1_7_0,
            ZaakResource.ZaakObject,
            Read
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.AddAsync),
            Post,
            V1_5Routes.ZaakObjecten.Create,
            V1_7_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.UpdateAsync),
            Put,
            V1_5Routes.ZaakObjecten.Update,
            V1_7_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.ZaakObjecten.Update,
            V1_7_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakObjectenController>(
            nameof(V1_7.ZaakObjectenController.DeleteAsync),
            DeleteMethod,
            V1_5Routes.ZaakObjecten.Delete,
            V1_7_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/7/ZaakResultatenController.cs
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.GetAllAsync),
            Get,
            V1Routes.ZaakResultaten.GetAll,
            V1_7_0,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.GetAsync),
            Get,
            V1Routes.ZaakResultaten.Get,
            V1_7_0,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.HeadAsync),
            Head,
            V1Routes.ZaakResultaten.Get,
            V1_7_0,
            ZaakResource.Resultaat,
            Read
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.AddAsync),
            Post,
            V1Routes.ZaakResultaten.Create,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.UpdateAsync),
            Put,
            V1Routes.ZaakResultaten.Update,
            V1_7_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.PartialUpdateAsync),
            Patch,
            V1Routes.ZaakResultaten.Update,
            V1_7_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakResultatenController>(
            nameof(V1_7.ZaakResultatenController.DeleteAsync),
            DeleteMethod,
            V1Routes.ZaakResultaten.Delete,
            V1_7_0,
            ZaakResource.Resultaat,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/7/ZaakRollenController.cs
        Row<V1_7.ZaakRollenController>(
            nameof(V1_7.ZaakRollenController.GetAllAsync),
            Get,
            V1Routes.ZaakRollen.GetAll,
            V1_7_0,
            ZaakResource.Rol,
            Read
        ),
        Row<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.GetAsync), Get, V1Routes.ZaakRollen.Get, V1_7_0, ZaakResource.Rol, Read),
        Row<V1_7.ZaakRollenController>(nameof(V1_7.ZaakRollenController.HeadAsync), Head, V1Routes.ZaakRollen.Get, V1_7_0, ZaakResource.Rol, Read),
        Row<V1_7.ZaakRollenController>(
            nameof(V1_7.ZaakRollenController.AddAsync),
            Post,
            V1Routes.ZaakRollen.Create,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZaakRollenController>(
            nameof(V1_7.ZaakRollenController.DeleteAsync),
            DeleteMethod,
            V1Routes.ZaakRollen.Delete,
            V1_7_0,
            ZaakResource.Rol,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/7/ZaakStatussenController.cs
        Row<V1_7.ZaakStatussenController>(
            nameof(V1_7.ZaakStatussenController.GetAllAsync),
            Get,
            V1_5Routes.ZaakStatussen.GetAll,
            V1_7_0,
            ZaakResource.Status,
            Read
        ),
        Row<V1_7.ZaakStatussenController>(
            nameof(V1_7.ZaakStatussenController.GetAsync),
            Get,
            V1_5Routes.ZaakStatussen.Get,
            V1_7_0,
            ZaakResource.Status,
            Read
        ),
        Row<V1_7.ZaakStatussenController>(
            nameof(V1_7.ZaakStatussenController.HeadAsync),
            Head,
            V1_5Routes.ZaakStatussen.Get,
            V1_7_0,
            ZaakResource.Status,
            Read
        ),
        Row<V1_7.ZaakStatussenController>(
            nameof(V1_7.ZaakStatussenController.AddAsync),
            Post,
            V1_5Routes.ZaakStatussen.Create,
            V1_7_0,
            ZaakResource.None,
            Create,
            AddStatus,
            Reopen
        ),
        // Controllers/v1/7/ZakenController.cs
        Row<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetAllAsync), Get, V1_5Routes.Zaken.GetAll, V1_7_0, ZaakResource.None, Read),
        Row<V1_7.ZakenController>(nameof(V1_7.ZakenController.SearchAsync), Post, V1_5Routes.Zaken.Search, V1_7_0, ZaakResource.None, Read),
        Row<V1_7.ZakenController>(nameof(V1_7.ZakenController.AddAsync), Post, V1_5Routes.Zaken.Create, V1_7_0, ZaakResource.None, Create),
        Row<V1_7.ZakenController>(nameof(V1_7.ZakenController.GetAsync), Get, V1_5Routes.Zaken.Get, V1_7_0, ZaakResource.None, Read),
        Row<V1_7.ZakenController>(nameof(V1_7.ZakenController.HeadAsync), Head, V1_5Routes.Zaken.Get, V1_7_0, ZaakResource.None, Read),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.UpdateAsync),
            Put,
            V1_5Routes.Zaken.Update,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.PartialUpdateAsync),
            Patch,
            V1_5Routes.Zaken.Update,
            V1_7_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.GetAllZaakBesluitenAsync),
            Get,
            V1Routes.ZaakBesluiten.GetAll,
            V1_7_0,
            ZaakResource.Besluit,
            Read
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.GetZaakBesluitAsync),
            Get,
            V1Routes.ZaakBesluiten.Get,
            V1_7_0,
            ZaakResource.Besluit,
            Read
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.GetAllZaakEigenschappenAsync),
            Get,
            V1Routes.ZaakEigenschappen.GetAll,
            V1_7_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.GetZaakEigenschapAsync),
            Get,
            V1_5Routes.ZaakEigenschappen.Get,
            V1_7_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1_7.ZakenController>(
            nameof(V1_7.ZakenController.HeadZaakEigenschapAsync),
            Head,
            V1_5Routes.ZaakEigenschappen.Get,
            V1_7_0,
            ZaakResource.Eigenschap,
            Read
        ),
    ];

    private static readonly Dictionary<string, ScopeMatrixRow> RowsByName = Rows.ToDictionary(r => r.Name);

    public static ScopeMatrixRow Named(string name) => RowsByName[name];

    public static string Name<TController>(string action) => ScopeMatrixRow.NameOf(typeof(TController), action);

    private static ScopeMatrixRow Row<TController>(
        string action,
        HttpMethod method,
        string route,
        string apiVersion,
        ZaakResource seeds,
        params string[] scopes
    )
    {
        return new ScopeMatrixRow(typeof(TController), action, method, route, apiVersion, scopes, seeds);
    }
}
