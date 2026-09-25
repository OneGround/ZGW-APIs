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

        // Lightweight path validation for use in the controller
        services.AddScoped(sp => new ExpandValidator<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Generic dispatcher for use in the controller
        services.AddScoped(sp => new ExpandEngine<ZaakResponseDto>(sp.GetServices<IExpandResolver<ZaakResponseDto>>()));

        // Note: Also registered by the [Obsolete] v1.5 AddExpandables() -- v1.7 must not silently
        // rely on that, or it breaks the moment AddExpandables() is ever removed.
        services.AddScoped<IGenericCache<ZaakTypeResponseDto>, GenericCache<ZaakTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
    }
}
