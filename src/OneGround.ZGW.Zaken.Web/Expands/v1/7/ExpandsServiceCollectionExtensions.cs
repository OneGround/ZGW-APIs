using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

public static class ExpandsServiceCollectionExtensions
{
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

        // Lightweight path validation for use in the controller
        services.AddScoped(sp => new ExpandValidator<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Generic dispatcher for use in the controller
        services.AddScoped(sp => new ExpandEngine<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Note: Also registered by the [Obsolete] v1.5 AddExpandables() -- v1.7 must not silently
        // rely on that, or it breaks the moment AddExpandables() is ever removed.
        services.AddScoped<IGenericCache<ZaakTypeResponseDto>, GenericCache<ZaakTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
    }

    /// <summary>
    /// Expand support for the STATUS resource itself (GET /statussen, GET /statussen/{uuid}).
    /// No <c>fields</c>/field-selection mechanism here -- unlike Zaken, the ZRC 1.7.0 spec doesn't add
    /// one for statussen, only "expand".
    /// </summary>
    public static void AddStatussenAPIExpands(this IServiceCollection services)
    {
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusZaakResolver>();
        services.AddScoped<IExpandResolver<StatusResponseDto>, StatusStatusTypeResolver>();

        services.AddScoped(sp => new ExpandValidator<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));

        services.AddScoped<IGenericCache<StatusTypeResponseDto>, GenericCache<StatusTypeResponseDto>>();
    }

    /// <summary>
    /// Expand support for the RESULTAAT resource itself (GET /resultaten, GET /resultaten/{uuid}).
    /// No <c>fields</c>/field-selection mechanism here -- same reasoning as
    /// <see cref="AddStatussenAPIExpands"/>.
    /// </summary>
    public static void AddResultatenAPIExpands(this IServiceCollection services)
    {
        services.AddScoped<IExpandResolver<ResultaatResponseDto>, ResultaatZaakResolver>();
        services.AddScoped<IExpandResolver<ResultaatResponseDto>, ResultaatResultaatTypeResolver>();

        services.AddScoped(sp => new ExpandValidator<ResultaatResponseDto>(sp.GetServices<IExpandResolver<ResultaatResponseDto>>()));
        services.AddScoped(sp => new ExpandEngine<ResultaatResponseDto>(sp.GetServices<IExpandResolver<ResultaatResponseDto>>()));

        services.AddScoped<IGenericCache<ResultaatTypeResponseDto>, GenericCache<ResultaatTypeResponseDto>>();
    }
}
