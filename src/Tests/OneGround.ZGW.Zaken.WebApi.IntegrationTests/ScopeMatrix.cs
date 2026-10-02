using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Controllers;
using V1 = OneGround.ZGW.Zaken.Web.Controllers.v1;
using V1_2 = OneGround.ZGW.Zaken.Web.Controllers.v1._2;
using V1_5 = OneGround.ZGW.Zaken.Web.Controllers.v1._5;

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
    public string Name => $"{Controller.FullName!["OneGround.ZGW.Zaken.Web.Controllers.".Length..]}.{Action}";

    public override string ToString() => Name;
}

/// <summary>
/// The hand-written scope table for every <c>[Scope]</c>-carrying action in the Zaken API, copied from the attributes as they are.
/// <see cref="ScopeMatrixCompletenessTests"/> fails when it drifts from them.
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

    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Head = HttpMethod.Head;
    private static readonly HttpMethod Post = HttpMethod.Post;
    private static readonly HttpMethod Put = HttpMethod.Put;
    private static readonly HttpMethod Patch = HttpMethod.Patch;
    private static readonly HttpMethod DeleteMethod = HttpMethod.Delete;

    public static readonly IReadOnlyList<ScopeMatrixRow> Rows =
    [
        // Controllers/v1/KlantContactenController.cs
        Row<V1.KlantContactenController>("GetAllAsync", Get, "api/v1/klantcontacten", V1_0, ZaakResource.KlantContact, Read),
        Row<V1.KlantContactenController>("GetAsync", Get, "api/v1/klantcontacten/{id}", V1_0, ZaakResource.KlantContact, Read),
        Row<V1.KlantContactenController>("AddAsync", Post, "api/v1/klantcontacten", V1_0, ZaakResource.None, Update, ForcedUpdate),
        // Controllers/v1/ZaakInformatieObjectenController.cs
        Row<V1.ZaakInformatieObjectenController>("GetAllAsync", Get, "api/v1/zaakinformatieobjecten", V1_0, ZaakResource.ZaakInformatieObject, Read),
        Row<V1.ZaakInformatieObjectenController>(
            "GetAsync",
            Get,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1.ZaakInformatieObjectenController>(
            "AddAsync",
            Post,
            "api/v1/zaakinformatieobjecten",
            V1_0,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            "UpdateAsync",
            Put,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            "PartialUpdateAsync",
            Patch,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZaakInformatieObjectenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_0,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/ZaakObjectenController.cs
        Row<V1.ZaakObjectenController>("GetAllAsync", Get, "api/v1/zaakobjecten", V1_0, ZaakResource.ZaakObject, Read),
        Row<V1.ZaakObjectenController>("GetAsync", Get, "api/v1/zaakobjecten/{id}", V1_0, ZaakResource.ZaakObject, Read),
        Row<V1.ZaakObjectenController>("AddAsync", Post, "api/v1/zaakobjecten", V1_0, ZaakResource.None, Create, Update, ForcedUpdate),
        // Controllers/v1/ZaakResultatenController.cs
        Row<V1.ZaakResultatenController>("GetAllAsync", Get, "api/v1/resultaten", V1_0, ZaakResource.Resultaat, Read),
        Row<V1.ZaakResultatenController>("GetAsync", Get, "api/v1/resultaten/{id}", V1_0, ZaakResource.Resultaat, Read),
        Row<V1.ZaakResultatenController>("AddAsync", Post, "api/v1/resultaten", V1_0, ZaakResource.None, Update, ForcedUpdate),
        Row<V1.ZaakResultatenController>("UpdateAsync", Put, "api/v1/resultaten/{id}", V1_0, ZaakResource.Resultaat, Update, ForcedUpdate),
        Row<V1.ZaakResultatenController>("PartialUpdateAsync", Patch, "api/v1/resultaten/{id}", V1_0, ZaakResource.Resultaat, Update, ForcedUpdate),
        Row<V1.ZaakResultatenController>("DeleteAsync", DeleteMethod, "api/v1/resultaten/{id}", V1_0, ZaakResource.Resultaat, Update, ForcedUpdate),
        // Controllers/v1/ZaakRollenController.cs
        Row<V1.ZaakRollenController>("GetAllAsync", Get, "api/v1/rollen", V1_0, ZaakResource.Rol, Read),
        Row<V1.ZaakRollenController>("GetAsync", Get, "api/v1/rollen/{id}", V1_0, ZaakResource.Rol, Read),
        Row<V1.ZaakRollenController>("AddAsync", Post, "api/v1/rollen", V1_0, ZaakResource.None, Update, ForcedUpdate),
        Row<V1.ZaakRollenController>("DeleteAsync", DeleteMethod, "api/v1/rollen/{id}", V1_0, ZaakResource.Rol, Update, ForcedUpdate),
        // Controllers/v1/ZaakStatussenController.cs
        Row<V1.ZaakStatussenController>("GetAllAsync", Get, "api/v1/statussen", V1_0, ZaakResource.Status, Read),
        Row<V1.ZaakStatussenController>("GetAsync", Get, "api/v1/statussen/{id}", V1_0, ZaakResource.Status, Read),
        Row<V1.ZaakStatussenController>("AddAsync", Post, "api/v1/statussen", V1_0, ZaakResource.None, Create, AddStatus, Reopen),
        // Controllers/v1/ZakenController.cs
        Row<V1.ZakenController>("GetAllAsync", Get, "api/v1/zaken", V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>("GetAsync", Get, "api/v1/zaken/{id}", V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>("SearchAsync", Post, "api/v1/zaken/_zoek", V1_0, ZaakResource.None, Read),
        Row<V1.ZakenController>("AddAsync", Post, "api/v1/zaken", V1_0, ZaakResource.None, Create),
        Row<V1.ZakenController>("UpdateAsync", Put, "api/v1/zaken/{id}", V1_0, ZaakResource.None, Update, ForcedUpdate),
        Row<V1.ZakenController>("PartialUpdateAsync", Patch, "api/v1/zaken/{id}", V1_0, ZaakResource.None, Update, ForcedUpdate),
        Row<V1.ZakenController>("DeleteAsync", DeleteMethod, "api/v1/zaken/{id}", V1_0, ZaakResource.None, Delete),
        Row<V1.ZakenController>(
            "GetAllZaakAuditTrailRegelsAsync",
            Get,
            "api/v1/zaken/{zaak_uuid}/audittrail",
            V1_0,
            ZaakResource.AuditTrailRegel,
            AuditTrailRead
        ),
        Row<V1.ZakenController>(
            "GetZaakAuditTrailRegelAsync",
            Get,
            "api/v1/zaken/{zaak_uuid}/audittrail/{uuid}",
            V1_0,
            ZaakResource.AuditTrailRegel,
            AuditTrailRead
        ),
        Row<V1.ZakenController>("GetAllZaakBesluitenAsync", Get, "api/v1/zaken/{zaak_uuid}/besluiten", V1_0, ZaakResource.Besluit, Read),
        Row<V1.ZakenController>("AddZaakBesluitenAsync", Post, "api/v1/zaken/{zaak_uuid}/besluiten", V1_0, ZaakResource.None, Update),
        Row<V1.ZakenController>("GetZaakBesluitAsync", Get, "api/v1/zaken/{zaak_uuid}/besluiten/{uuid}", V1_0, ZaakResource.Besluit, Read),
        Row<V1.ZakenController>(
            "DeleteZaakBesluitAsync",
            DeleteMethod,
            "api/v1/zaken/{zaak_uuid}/besluiten/{uuid}",
            V1_0,
            ZaakResource.Besluit,
            Update
        ),
        Row<V1.ZakenController>(
            "GetAllZaakEigenschappenAsync",
            Get,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen",
            V1_0,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1.ZakenController>(
            "AddZaakEigenschapAsync",
            Post,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen",
            V1_0,
            ZaakResource.None,
            Update,
            ForcedUpdate
        ),
        Row<V1.ZakenController>(
            "GetZaakEigenschapAsync",
            Get,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_0,
            ZaakResource.Eigenschap,
            Read
        ),
        // Controllers/v1/2/ZaakObjectenController.cs
        Row<V1_2.ZaakObjectenController>("GetAllAsync", Get, "api/v1/zaakobjecten", V1_2_0, ZaakResource.ZaakObject, Read),
        Row<V1_2.ZaakObjectenController>("GetAsync", Get, "api/v1/zaakobjecten/{id}", V1_2_0, ZaakResource.ZaakObject, Read),
        Row<V1_2.ZaakObjectenController>("AddAsync", Post, "api/v1/zaakobjecten", V1_2_0, ZaakResource.None, Create, Update, ForcedUpdate),
        Row<V1_2.ZaakObjectenController>("UpdateAsync", Put, "api/v1/zaakobjecten/{id}", V1_2_0, ZaakResource.ZaakObject, Update, ForcedUpdate),
        Row<V1_2.ZaakObjectenController>(
            "PartialUpdateAsync",
            Patch,
            "api/v1/zaakobjecten/{id}",
            V1_2_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZaakObjectenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakobjecten/{id}",
            V1_2_0,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/2/ZakenController.cs
        Row<V1_2.ZakenController>(
            "UpdateAsync",
            Put,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZakenController>(
            "PartialUpdateAsync",
            Patch,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        Row<V1_2.ZakenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_2_0,
            ZaakResource.Eigenschap,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZaakContactMomentenController.cs
        Row<V1_5.ZaakContactmomentenController>("GetAllAsync", Get, "api/v1/zaakcontactmomenten", V1_5_1, ZaakResource.Contactmoment, Read),
        Row<V1_5.ZaakContactmomentenController>("GetAsync", Get, "api/v1/zaakcontactmomenten/{id}", V1_5_1, ZaakResource.Contactmoment, Read),
        Row<V1_5.ZaakContactmomentenController>("AddAsync", Post, "api/v1/zaakcontactmomenten", V1_5_1, ZaakResource.None, Update, ForcedUpdate),
        Row<V1_5.ZaakContactmomentenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakcontactmomenten/{id}",
            V1_5_1,
            ZaakResource.Contactmoment,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZaakInformatieObjectenController.cs
        Row<V1_5.ZaakInformatieObjectenController>(
            "GetAllAsync",
            Get,
            "api/v1/zaakinformatieobjecten",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "GetAsync",
            Get,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "HeadAsync",
            Head,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Read
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "AddAsync",
            Post,
            "api/v1/zaakinformatieobjecten",
            V1_5_1,
            ZaakResource.None,
            Create,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "UpdateAsync",
            Put,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "PartialUpdateAsync",
            Patch,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakInformatieObjectenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakinformatieobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakInformatieObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/5/ZaakObjectenController.cs
        Row<V1_5.ZaakObjectenController>("GetAllAsync", Get, "api/v1/zaakobjecten", V1_5_1, ZaakResource.ZaakObject, Read),
        Row<V1_5.ZaakObjectenController>("GetAsync", Get, "api/v1/zaakobjecten/{id}", V1_5_1, ZaakResource.ZaakObject, Read),
        Row<V1_5.ZaakObjectenController>("HeadAsync", Head, "api/v1/zaakobjecten/{id}", V1_5_1, ZaakResource.ZaakObject, Read),
        Row<V1_5.ZaakObjectenController>("AddAsync", Post, "api/v1/zaakobjecten", V1_5_1, ZaakResource.None, Create, Update, ForcedUpdate),
        Row<V1_5.ZaakObjectenController>("UpdateAsync", Put, "api/v1/zaakobjecten/{id}", V1_5_1, ZaakResource.ZaakObject, Update, ForcedUpdate),
        Row<V1_5.ZaakObjectenController>(
            "PartialUpdateAsync",
            Patch,
            "api/v1/zaakobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate
        ),
        Row<V1_5.ZaakObjectenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakobjecten/{id}",
            V1_5_1,
            ZaakResource.ZaakObject,
            Update,
            ForcedUpdate,
            Delete
        ),
        // Controllers/v1/5/ZaakResultatenController.cs
        Row<V1_5.ZaakResultatenController>("GetAsync", Get, "api/v1/resultaten/{id}", V1_5_1, ZaakResource.Resultaat, Read),
        Row<V1_5.ZaakResultatenController>("HeadAsync", Head, "api/v1/resultaten/{id}", V1_5_1, ZaakResource.Resultaat, Read),
        // Controllers/v1/5/ZaakRollenController.cs
        Row<V1_5.ZaakRollenController>("GetAllAsync", Get, "api/v1/rollen", V1_5_1, ZaakResource.Rol, Read),
        Row<V1_5.ZaakRollenController>("GetAsync", Get, "api/v1/rollen/{id}", V1_5_1, ZaakResource.Rol, Read),
        Row<V1_5.ZaakRollenController>("HeadAsync", Head, "api/v1/rollen/{id}", V1_5_1, ZaakResource.Rol, Read),
        Row<V1_5.ZaakRollenController>("AddAsync", Post, "api/v1/rollen", V1_5_1, ZaakResource.None, Update, ForcedUpdate),
        Row<V1_5.ZaakRollenController>("DeleteAsync", DeleteMethod, "api/v1/rollen/{id}", V1_5_1, ZaakResource.Rol, Update, ForcedUpdate),
        // Controllers/v1/5/ZaakStatussenController.cs
        Row<V1_5.ZaakStatussenController>("GetAllAsync", Get, "api/v1/statussen", V1_5_1, ZaakResource.Status, Read),
        Row<V1_5.ZaakStatussenController>("GetAsync", Get, "api/v1/statussen/{id}", V1_5_1, ZaakResource.Status, Read),
        Row<V1_5.ZaakStatussenController>("HeadAsync", Head, "api/v1/statussen/{id}", V1_5_1, ZaakResource.Status, Read),
        Row<V1_5.ZaakStatussenController>("AddAsync", Post, "api/v1/statussen", V1_5_1, ZaakResource.None, Create, AddStatus, Reopen),
        // Controllers/v1/5/ZaakVerzoekenController.cs
        Row<V1_5.ZaakVerzoekenController>("GetAllAsync", Get, "api/v1/zaakverzoeken", V1_5_1, ZaakResource.Verzoek, Read),
        Row<V1_5.ZaakVerzoekenController>("GetAsync", Get, "api/v1/zaakverzoeken/{id}", V1_5_1, ZaakResource.Verzoek, Read),
        Row<V1_5.ZaakVerzoekenController>("AddAsync", Post, "api/v1/zaakverzoeken", V1_5_1, ZaakResource.None, Update, ForcedUpdate),
        Row<V1_5.ZaakVerzoekenController>(
            "DeleteAsync",
            DeleteMethod,
            "api/v1/zaakverzoeken/{id}",
            V1_5_1,
            ZaakResource.Verzoek,
            Update,
            ForcedUpdate
        ),
        // Controllers/v1/5/ZakenController.cs
        Row<V1_5.ZakenController>("GetAllAsync", Get, "api/v1/zaken", V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>("SearchAsync", Post, "api/v1/zaken/_zoek", V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>("AddAsync", Post, "api/v1/zaken", V1_5_1, ZaakResource.None, Create),
        Row<V1_5.ZakenController>("GetAsync", Get, "api/v1/zaken/{id}", V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>("HeadAsync", Head, "api/v1/zaken/{id}", V1_5_1, ZaakResource.None, Read),
        Row<V1_5.ZakenController>("UpdateAsync", Put, "api/v1/zaken/{id}", V1_5_1, ZaakResource.None, Update, ForcedUpdate),
        Row<V1_5.ZakenController>("PartialUpdateAsync", Patch, "api/v1/zaken/{id}", V1_5_1, ZaakResource.None, Update, ForcedUpdate),
        Row<V1_5.ZakenController>(
            "GetZaakEigenschapAsync",
            Get,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_5_1,
            ZaakResource.Eigenschap,
            Read
        ),
        Row<V1_5.ZakenController>(
            "HeadZaakEigenschapAsync",
            Head,
            "api/v1/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}",
            V1_5_1,
            ZaakResource.Eigenschap,
            Read
        ),
    ];

    private static readonly Dictionary<string, ScopeMatrixRow> RowsByName = Rows.ToDictionary(r => r.Name);

    public static ScopeMatrixRow Named(string name) => RowsByName[name];

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
