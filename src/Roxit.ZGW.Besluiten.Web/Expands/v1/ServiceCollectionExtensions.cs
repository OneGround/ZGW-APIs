using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Web.Expands;

namespace Roxit.ZGW.Besluiten.Web.Expands.v1;

public static class ServiceCollectionExtensions
{
    public static void AddExpandables(this IServiceCollection services)
    {
        services.AddScoped<IExpanderFactory, ExpanderFactory>();

        services.AddScoped<IObjectExpander<BesluitResponseDto>, BesluitExpander>();
        services.AddScoped<IObjectExpander<string>, BesluitTypeExpander>();
        services.AddScoped<IObjectExpander<string>, BesluitInformatieObjectenExpander>();

        // Note: important to be registered as scoped. So cached within the context of one request (with expand). So this solves the n+1 problem🙂
        services.AddScoped<IGenericCache<BesluitTypeResponseDto>, GenericCache<BesluitTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
        services.AddScoped<IGenericCache<object>, GenericCache<object>>(); // Note: DRC response containing dynamically created expands
    }
}
