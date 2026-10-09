using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

public static class ExpandsServiceCollectionExtensions
{
    // Note: per request, so the same zaak / status / document / OIO list is fetched once for all rows that expand to it. Every method that
    // registers a resolver that needs one of these registers it itself (TryAdd, so it does not matter which are called, or in which order).
    private static void AddZaakLookup(IServiceCollection services)
    {
        services.TryAddScoped<IGenericCache<QueryResult<Zaak>>, GenericCache<QueryResult<Zaak>>>();
        services.TryAddScoped<IZaakLookup, ZaakLookup>();
    }

    private static void AddObjectInformatieObjectenCache(IServiceCollection services)
    {
        services.TryAddScoped<
            IGenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>>,
            GenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>>
        >();
    }

    private static void AddZaakInformatieObjectCaches(IServiceCollection services)
    {
        services.TryAddScoped<IGenericCache<QueryResult<ZaakStatus>>, GenericCache<QueryResult<ZaakStatus>>>();
        services.TryAddScoped<
            IGenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>>,
            GenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>>
        >();
    }

    public static void AddZakenAPIFieldsValidators(this IServiceCollection services)
    {
        services.AddScoped(sp =>
        {
            var expandablePaths = sp.GetServices<IExpandResolver<ZaakResponseDto>>()
                .SelectMany(r => r.AdditionalPaths.Select(a => a.Path).Prepend(r.Path));

            return new FieldsValidator<ZaakResponseDto>(ZaakFieldsSchema.Build(), expandablePaths);
        });
    }

    public static void AddZakenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        AddObjectInformatieObjectenCache(services);
        // Expand resolvers -- one per expand path, registered as IExpandResolver<ZaakResponseDto>
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakTypeResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakTypeCatalogusResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakStatusResolver>();
        // Note: reuses IGenericCache<StatusTypeResponseDto>, registered below by AddStatussenAPIExpands
        // (both are called together in Startup.cs) -- see ZaakStatusStatusTypeResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakStatusStatusTypeResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakResultaatResolver>();
        // Note: reuses IGenericCache<ResultaatTypeResponseDto>, registered below by
        // AddResultatenAPIExpands (both are called together in Startup.cs).
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakResultaatResultaatTypeResolver>();
        // Note: reuses ExpandEngine<RolResponseDto>, registered below by AddRollenAPIExpands (both
        // are called together in Startup.cs) -- see ZaakRollenResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakRollenResolver>();
        // Note: reuses ExpandEngine<ZaakObjectResponseDto>, registered below by
        // AddZaakObjectenAPIExpands (both are called together in Startup.cs) -- see
        // ZaakZaakObjectenResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakObjectenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakContactmomentenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakVerzoekenResolver>();
        // Note: reuses ExpandEngine<ZaakEigenschapResponseDto>, registered below by
        // AddZaakEigenschappenAPIExpands (both are called together in Startup.cs) -- see
        // ZaakEigenschappenResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakEigenschappenResolver>();
        // Note: reuses ExpandEngine<ZaakInformatieObjectResponseDto>, registered below by
        // AddZaakInformatieObjectenAPIExpands (both are called together in Startup.cs) -- see
        // ZaakZaakInformatieObjectenResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakInformatieObjectenResolver>();
        // Note: reuses the ExpandEngine<ZaakResponseDto> registered right below, as a Lazy<T> -- see
        // ZaakHoofdzaakResolver's own remarks for why.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakHoofdzaakResolver>();
        // Note: reuses the same ExpandEngine<ZaakResponseDto> as ZaakHoofdzaakResolver, for
        // "deelzaken.*" -- but resolved lazily via IServiceProvider, not as a Lazy<T> constructor
        // dependency, since IServiceProvider is already needed here for CreateScope (see
        // ZaakDeelzakenResolver's own remarks).
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakDeelzakenResolver>();
        // Note: same ExpandEngine<ZaakResponseDto> reuse/IServiceProvider style as ZaakDeelzakenResolver,
        // for "relevanteanderezaken.*" -- see ZaakRelevanteAndereZakenResolver's own remarks.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakRelevanteAndereZakenResolver>();
        // Note: both need IExternalJsonClient, registered by AddExternalJsonClient in Startup.cs.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakCommunicatiekanaalResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakSelectielijstklasseResolver>();

        // Lightweight path validation for use in the controller
        services.AddScoped(sp => new ExpandValidator<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Generic dispatcher for use in the controller
        services.AddScoped(sp => new ExpandEngine<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Lazy wrapper for ZaakHoofdzaakResolver: defers resolving this same engine until first
        // actually needed, so it can be a plain constructor dependency instead of a service-locator
        // call (the engine's own construction needs every IExpandResolver<ZaakResponseDto>, including
        // ZaakHoofdzaakResolver, so a direct, eager dependency would be circular).
        services.AddScoped(sp => new Lazy<ExpandEngine<ZaakResponseDto>>(() => sp.GetRequiredService<ExpandEngine<ZaakResponseDto>>()));

        // Note: Also registered by the [Obsolete] v1.5 AddExpandables() -- v1.7 must not silently
        // rely on that, or it breaks the moment AddExpandables() is ever removed.
        services.AddScoped<IGenericCache<ZaakTypeResponseDto>, GenericCache<ZaakTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
    }

    /// <summary>
    /// Expand support for the STATUS resource itself (GET /statussen, GET /statussen/{uuid}).
    /// No <c>fields</c>/field-selection mechanism here -- unlike Zaken, the ZRC 1.7.0 spec doesn't add
    /// one for statussen, only "expand". "zaakinformatieobjecten" is same-service (via the existing
    /// <c>GetAllZaakInformatieObjectenQuery</c>, extended with a <c>Status</c> filter option) cross-checked
    /// against DRC's authorization model, and forwards "...informatieobject"/"...informatieobjecttype" to
    /// the <see cref="ExpandEngine{TEntity}"/> registered below by AddZaakInformatieObjectenAPIExpands
    /// (both are called together in Startup.cs) -- see <see cref="StatusZaakInformatieObjectenResolver"/>.
    /// "zaak.zaaktype" is forwarded to the <see cref="Lazy{T}"/>-wrapped <see cref="ExpandEngine{TEntity}"/>
    /// of <see cref="ZaakResponseDto"/> registered by AddZakenAPIExpands -- see
    /// <see cref="StatusZaakResolver"/>. This method also registers a <see cref="Lazy{T}"/> wrapper
    /// around its own <see cref="ExpandEngine{TEntity}"/> of <see cref="StatusResponseDto"/>, for
    /// <see cref="ZaakInformatieObjectStatusResolver"/> to forward "status.statustype" from the
    /// ZAAKINFORMATIEOBJECT resource (registered by AddZaakInformatieObjectenAPIExpands, called
    /// alongside this one in Startup.cs). Unlike the "zaak.zaaktype" case, this <see cref="Lazy{T}"/> is
    /// not just an optimization: <see cref="StatusZaakInformatieObjectenResolver"/>'s own eager
    /// dependency on <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakInformatieObjectResponseDto"/>
    /// closes a genuine cycle back to this engine -- see
    /// <see cref="ZaakInformatieObjectStatusResolver"/>'s own remarks for the full chain.
    /// </summary>
    public static void AddStatussenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        AddObjectInformatieObjectenCache(services);
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusZaakResolver>();
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusStatusTypeResolver>();
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusZaakInformatieObjectenResolver>();

        services.AddScoped(sp => new ExpandValidator<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
        services.AddScoped(sp => new Lazy<ExpandEngine<StatusResponseDto>>(() => sp.GetRequiredService<ExpandEngine<StatusResponseDto>>()));

        services.AddScoped<IGenericCache<StatusTypeResponseDto>, GenericCache<StatusTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the RESULTAAT resource itself (GET /resultaten, GET /resultaten/{uuid}).
    /// No <c>fields</c>/field-selection mechanism here -- same reasoning as
    /// <see cref="AddStatussenAPIExpands"/>. "zaak.zaaktype" is forwarded to the <see cref="Lazy{T}"/>-wrapped
    /// <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/> registered by
    /// AddZakenAPIExpands -- see <see cref="ResultaatZaakResolver"/>.
    /// </summary>
    public static void AddResultatenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<ResultaatResponseDto>, ResultaatZaakResolver>();
        services.AddScoped<IExpandResolver<ResultaatResponseDto>, ResultaatResultaatTypeResolver>();

        services.AddScoped(sp => new ExpandValidator<ResultaatResponseDto>(sp.GetServices<IExpandResolver<ResultaatResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ResultaatResponseDto>(sp.GetServices<IExpandResolver<ResultaatResponseDto>>()));

        services.AddScoped<IGenericCache<ResultaatTypeResponseDto>, GenericCache<ResultaatTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the ROL resource itself (GET /rollen, GET /rollen/{uuid}). No <c>fields</c>
    /// mechanism here -- same reasoning as <see cref="AddStatussenAPIExpands"/>. This
    /// <see cref="ExpandEngine{TEntity}"/> is also reused directly by <see cref="ZaakRollenResolver"/>
    /// to resolve "rollen.roltype" per item of the "rollen" list on a ZAAK. "zaak.zaaktype" is forwarded
    /// to the <see cref="Lazy{T}"/>-wrapped <see cref="ExpandEngine{TEntity}"/> of
    /// <see cref="ZaakResponseDto"/> registered by AddZakenAPIExpands -- see <see cref="RolZaakResolver"/>.
    /// </summary>
    public static void AddRollenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<RolResponseDto>, RolZaakResolver>();
        services.AddScoped<IExpandResolver<RolResponseDto>, RolRolTypeResolver>();

        services.AddScoped(sp => new ExpandValidator<RolResponseDto>(sp.GetServices<IExpandResolver<RolResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<RolResponseDto>(sp.GetServices<IExpandResolver<RolResponseDto>>()));

        services.AddScoped<IGenericCache<RolTypeResponseDto>, GenericCache<RolTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the ZAAKOBJECT resource itself (GET /zaakobjecten, GET /zaakobjecten/{uuid}).
    /// No <c>fields</c> mechanism here -- same reasoning as <see cref="AddStatussenAPIExpands"/>. This
    /// <see cref="ExpandEngine{TEntity}"/> is also reused directly by
    /// <see cref="ZaakZaakObjectenResolver"/> to resolve "zaakobjecten.zaakobjecttype" per item of the
    /// "zaakobjecten" list on a ZAAK. "zaak.zaaktype" is forwarded to the <see cref="Lazy{T}"/>-wrapped
    /// <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/> registered by
    /// AddZakenAPIExpands -- see <see cref="ZaakObjectZaakResolver"/>.
    /// </summary>
    public static void AddZaakObjectenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<ZaakObjectResponseDto>, ZaakObjectZaakResolver>();
        services.AddScoped<IExpandResolver<ZaakObjectResponseDto>, ZaakObjectZaakObjectTypeResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakObjectResponseDto>(sp.GetServices<IExpandResolver<ZaakObjectResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ZaakObjectResponseDto>(sp.GetServices<IExpandResolver<ZaakObjectResponseDto>>()));

        services.AddScoped<IGenericCache<ZaakObjectTypeResponseDto>, GenericCache<ZaakObjectTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the ZAAKCONTACTMOMENT resource itself (GET /zaakcontactmomenten, GET
    /// /zaakcontactmomenten/{uuid}). No <c>fields</c> mechanism here -- same reasoning as
    /// <see cref="AddStatussenAPIExpands"/>. Unlike ROL/ZAAKOBJECT, <see cref="ZaakZaakContactmomentenResolver"/>
    /// has no nested expand to resolve, so it does not reuse this <see cref="ExpandEngine{TEntity}"/>.
    /// </summary>
    public static void AddZaakContactmomentenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<ZaakContactmomentResponseDto>, ZaakContactmomentZaakResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakContactmomentResponseDto>(sp.GetServices<IExpandResolver<ZaakContactmomentResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ZaakContactmomentResponseDto>(sp.GetServices<IExpandResolver<ZaakContactmomentResponseDto>>()));
    }

    /// <summary>
    /// Expand support for the ZAAKEIGENSCHAP resource itself (GET
    /// /zaken/{zaak_uuid}/zaakeigenschappen, GET /zaken/{zaak_uuid}/zaakeigenschappen/{uuid} -- both
    /// actions live on ZakenController, not a dedicated controller). No <c>fields</c> mechanism here --
    /// same reasoning as <see cref="AddStatussenAPIExpands"/>. This <see cref="ExpandEngine{TEntity}"/>
    /// is also reused directly by <see cref="ZaakEigenschappenResolver"/> to resolve
    /// "eigenschappen.eigenschap" per item of the "eigenschappen" list on a ZAAK.
    /// </summary>
    public static void AddZaakEigenschappenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<ZaakEigenschapResponseDto>, ZaakEigenschapZaakResolver>();
        services.AddScoped<IExpandResolver<ZaakEigenschapResponseDto>, ZaakEigenschapEigenschapResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakEigenschapResponseDto>(sp.GetServices<IExpandResolver<ZaakEigenschapResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ZaakEigenschapResponseDto>(sp.GetServices<IExpandResolver<ZaakEigenschapResponseDto>>()));

        services.AddScoped<IGenericCache<EigenschapResponseDto>, GenericCache<EigenschapResponseDto>>();
    }

    /// <summary>
    /// Expand support for the ZAAKINFORMATIEOBJECT resource itself (GET /zaakinformatieobjecten, GET
    /// /zaakinformatieobjecten/{uuid}). No <c>fields</c> mechanism here -- same reasoning as
    /// <see cref="AddStatussenAPIExpands"/>. This <see cref="ExpandEngine{TEntity}"/> is also reused
    /// directly by <see cref="ZaakZaakInformatieObjectenResolver"/> to resolve
    /// "zaakinformatieobjecten.informatieobject" per item of the "zaakinformatieobjecten" list on a
    /// ZAAK. Unlike the other resources, "informatieobject" itself is DRC-backed (not
    /// ZTC) and calls the user-authenticated
    /// <see cref="OneGround.ZGW.Documenten.ServiceAgent.v1._7.IUserAuthDocumentenServiceAgent"/>
    /// (registered by AddUserAuthDocumentenServiceAgent_v1_7 in Startup.cs, not the obsolete v1._5
    /// variant used elsewhere specifically for v1._5 expands) rather than a ZTC-style decorator -- see
    /// ZaakInformatieObjectInformatieObjectResolver's own remarks for why.
    /// No <see cref="IGenericCache{T}"/> registration for that agent: it does not cache per-url like
    /// the plain (service-account-authenticated) v1._7 one does, so every "informatieobject" expand
    /// hits DRC fresh -- an accepted, un-optimized starting point (the same known N+1 as ZaakContactmomenten's "zaak" expand). "informatieobject.informatieobjecttype"
    /// (one level deeper still) IS ZTC-backed though, exactly like every other "...type" expand -- see <see cref="EnkelvoudigInformatieObjectInformatieObjectTypeResolver"/>'s own remarks.
    /// "status" (the optional status-at-time-of-filing reference) is same-service, resolved via the
    /// existing <c>GetZaakStatusQuery</c> -- see <see cref="ZaakInformatieObjectStatusResolver"/>.
    /// "zaak.zaaktype" is forwarded to the <see cref="Lazy{T}"/>-wrapped <see cref="ExpandEngine{TEntity}"/>
    /// of <see cref="ZaakResponseDto"/> registered by AddZakenAPIExpands -- see
    /// <see cref="ZaakInformatieObjectZaakResolver"/>. "status.statustype" is forwarded the same way to
    /// the <see cref="Lazy{T}"/>-wrapped <see cref="ExpandEngine{TEntity}"/> of <see cref="StatusResponseDto"/>
    /// registered by AddStatussenAPIExpands (also called alongside this one in Startup.cs) -- see
    /// <see cref="ZaakInformatieObjectStatusResolver"/>.
    /// </summary>
    public static void AddZaakInformatieObjectenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        AddZaakInformatieObjectCaches(services);
        services.AddScoped<IExpandResolver<ZaakInformatieObjectResponseDto>, ZaakInformatieObjectZaakResolver>();
        services.AddScoped<IExpandResolver<ZaakInformatieObjectResponseDto>, ZaakInformatieObjectInformatieObjectResolver>();
        services.AddScoped<IExpandResolver<ZaakInformatieObjectResponseDto>, ZaakInformatieObjectStatusResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakInformatieObjectResponseDto>(
            sp.GetServices<IExpandResolver<ZaakInformatieObjectResponseDto>>()
        ));
        services.AddScoped(sp => new ExpandEngine<ZaakInformatieObjectResponseDto>(
            sp.GetServices<IExpandResolver<ZaakInformatieObjectResponseDto>>()
        ));

        // "informatieobject.informatieobjecttype" -- ZTC-backed, resolved by
        // ZaakInformatieObjectInformatieObjectResolver via this engine, same shape as
        // ZaakTypeResolver's own ExpandEngine<ZaakResponseDto> reuse. No "...catalogus" sibling --
        // that would be a 4th nesting level from ZAAK, which exceeds the VNG ZGW spec's 3-level cap.
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>, EnkelvoudigInformatieObjectInformatieObjectTypeResolver>();
        services.AddScoped(sp => new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        services.AddScoped<IGenericCache<InformatieObjectTypeResponseDto>, GenericCache<InformatieObjectTypeResponseDto>>();
    }
}
