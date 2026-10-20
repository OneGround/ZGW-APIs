using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
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
    // Note: per request, so the same zaak / status / document is fetched once for all rows that expand to it. Every method that
    // registers a resolver that needs one of these registers it itself (TryAdd, so it does not matter which are called, or in which order).
    private static void AddZaakLookup(IServiceCollection services)
    {
        services.TryAddScoped<IGenericCache<QueryResult<Zaak>>, GenericCache<QueryResult<Zaak>>>();
        services.TryAddScoped<IZaakLookup, ZaakLookup>();
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
        // Note: one resolver per expand path. Some reuse the caches and engines that the other Add...APIExpands methods register (all of them
        // are called together in Startup.cs).
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakTypeResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakTypeCatalogusResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakStatusResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakStatusStatusTypeResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakResultaatResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakResultaatResultaatTypeResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakRollenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakObjectenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakContactmomentenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakVerzoekenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakEigenschappenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakZaakInformatieObjectenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakHoofdzaakResolver>();
        // Note: deelzaken and relevanteanderezaken take the engine of ZaakResponseDto from the IServiceProvider (they need it for CreateScope
        // anyway), hoofdzaak takes it as a Lazy<T>.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakDeelzakenResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakRelevanteAndereZakenResolver>();
        // Note: both need IExternalJsonClient, registered by AddExternalJsonClient in Startup.cs.
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakCommunicatiekanaalResolver>();
        services.AddScoped<IExpandResolver<ZaakResponseDto>, ZaakSelectielijstklasseResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        services.AddScoped(sp => new ExpandEngine<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Note: the engine needs every IExpandResolver<ZaakResponseDto>, ZaakHoofdzaakResolver included, so that resolver can only take it lazily
        services.AddScoped(sp => new Lazy<ExpandEngine<ZaakResponseDto>>(() => sp.GetRequiredService<ExpandEngine<ZaakResponseDto>>()));

        // Note: also registered by the [Obsolete] v1.5 AddExpandables(); v1.7 must not rely on that
        services.AddScoped<IGenericCache<ZaakTypeResponseDto>, GenericCache<ZaakTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
    }

    /// <summary>
    /// Expand support for the STATUS resource itself (GET /statussen, GET /statussen/{uuid}), without a <c>fields</c> mechanism (the spec adds
    /// none for statussen). "zaakinformatieobjecten" forwards "...informatieobject" to the engine of AddZaakInformatieObjectenAPIExpands.
    /// <para>
    /// Note: this method also registers a <see cref="Lazy{T}"/> of its own engine for <see cref="ZaakInformatieObjectStatusResolver"/>. That
    /// <see cref="Lazy{T}"/> is required: <see cref="StatusZaakInformatieObjectenResolver"/> depends on the engine of
    /// <see cref="ZaakInformatieObjectResponseDto"/>, which closes a cycle back to this engine.
    /// </para>
    /// </summary>
    public static void AddStatussenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusZaakResolver>();
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusStatusTypeResolver>();
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusZaakInformatieObjectenResolver>();

        services.AddScoped(sp => new ExpandValidator<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
        services.AddScoped(sp => new Lazy<ExpandEngine<StatusResponseDto>>(() => sp.GetRequiredService<ExpandEngine<StatusResponseDto>>()));

        services.AddScoped<IGenericCache<StatusTypeResponseDto>, GenericCache<StatusTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the RESULTAAT resource itself (GET /resultaten, GET /resultaten/{uuid}), without a <c>fields</c> mechanism.
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
    /// Expand support for the ROL resource itself (GET /rollen, GET /rollen/{uuid}), without a <c>fields</c> mechanism. The engine is also used
    /// by <see cref="ZaakRollenResolver"/> for "rollen.roltype".
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
    /// Expand support for the ZAAKOBJECT resource itself (GET /zaakobjecten, GET /zaakobjecten/{uuid}), without a <c>fields</c> mechanism. The
    /// engine is also used by <see cref="ZaakZaakObjectenResolver"/> for "zaakobjecten.zaakobjecttype".
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
    /// Expand support for the ZAAKCONTACTMOMENT resource itself (GET /zaakcontactmomenten, GET /zaakcontactmomenten/{uuid}), without a
    /// <c>fields</c> mechanism.
    /// </summary>
    public static void AddZaakContactmomentenAPIExpands(this IServiceCollection services)
    {
        AddZaakLookup(services);
        services.AddScoped<IExpandResolver<ZaakContactmomentResponseDto>, ZaakContactmomentZaakResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakContactmomentResponseDto>(sp.GetServices<IExpandResolver<ZaakContactmomentResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ZaakContactmomentResponseDto>(sp.GetServices<IExpandResolver<ZaakContactmomentResponseDto>>()));
    }

    /// <summary>
    /// Expand support for the ZAAKEIGENSCHAP resource itself (both actions are on ZakenController), without a <c>fields</c> mechanism. The
    /// engine is also used by <see cref="ZaakEigenschappenResolver"/> for "eigenschappen.eigenschap".
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
    /// Expand support for the ZAAKBESLUIT resource itself (both actions are on ZakenController), without a <c>fields</c> mechanism.
    /// "besluit" is BRC-backed through the user-authenticated <see cref="OneGround.ZGW.Besluiten.ServiceAgent.v1.IUserAuthBesluitenServiceAgent"/>,
    /// which has no per-url cache of its own, so the result is cached per request here.
    /// </summary>
    public static void AddZaakBesluitenAPIExpands(this IServiceCollection services)
    {
        services.TryAddScoped<IGenericCache<ServiceAgentResponse<BesluitResponseDto>>, GenericCache<ServiceAgentResponse<BesluitResponseDto>>>();
        services.AddScoped<IExpandResolver<ZaakBesluitResponseDto>, ZaakBesluitBesluitResolver>();

        services.AddScoped(sp => new ExpandValidator<ZaakBesluitResponseDto>(sp.GetServices<IExpandResolver<ZaakBesluitResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ZaakBesluitResponseDto>(sp.GetServices<IExpandResolver<ZaakBesluitResponseDto>>()));
    }

    /// <summary>
    /// Expand support for the ZAAKINFORMATIEOBJECT resource itself (GET /zaakinformatieobjecten, GET /zaakinformatieobjecten/{uuid}), without a
    /// <c>fields</c> mechanism. The engine is also used by <see cref="ZaakZaakInformatieObjectenResolver"/> for "informatieobject".
    /// <para>
    /// "informatieobject" is DRC-backed through the user-authenticated <see cref="OneGround.ZGW.Documenten.ServiceAgent.v1._7.IUserAuthDocumentenServiceAgent"/>,
    /// which has no per-url cache, so every "informatieobject" expand calls DRC (an accepted N+1). "informatieobject.informatieobjecttype" is
    /// ZTC-backed.
    /// </para>
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

        // Note: "informatieobject.informatieobjecttype" is ZTC-backed; no "...catalogus" (4th nesting level, over the 3-level cap of the spec)
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>, EnkelvoudigInformatieObjectInformatieObjectTypeResolver>();
        services.AddScoped(sp => new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        services.AddScoped<IGenericCache<InformatieObjectTypeResponseDto>, GenericCache<InformatieObjectTypeResponseDto>>();
    }
}
