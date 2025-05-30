using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;

namespace Roxit.ZGW.Documenten.Web.Expands.v1._5;

public static class ServiceCollectionExtensions
{
    public static void AddExpandables(this IServiceCollection services)
    {
        // Expanders support _expand in responses (>= v1.4)
        services.AddScoped<IExpanderFactory, ExpanderFactory>();

        services.AddScoped<IObjectExpander<EnkelvoudigInformatieObjectGetResponseDto>, EnkelvoudigInformatieObjectExpander>();
        services.AddScoped<IObjectExpander<string>, InformatieObjectTypeExpander>();
        services.AddScoped<IObjectExpander<InformatieObjectContext>, InformatieObjectExpander>();

        // Note: important to be registered as scoped. So cached within the context of one request (with expand). So this solves the n+1 problem🙂
        services.AddScoped<IGenericCache<InformatieObjectTypeResponseDto>, GenericCache<InformatieObjectTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
        services.AddScoped<IGenericCache<EnkelvoudigInformatieObjectGetResponseDto>, GenericCache<EnkelvoudigInformatieObjectGetResponseDto>>();
    }
}
