using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public static class ZaakExpanderExtensions
{
    public static void AddExpandables(this IServiceCollection services)
    {
        // Expanders support _expand in responses (>=v1.5)
        services.AddScoped<IExpanderFactory, ExpanderFactory>();

        services.AddScoped<IObjectExpander<ZaakResponseDto>, ZaakExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakTypeExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakStatusExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakResultaatExpander>();
        services.AddScoped<IObjectExpander<string>, HoofdZaakExpander>();
        services.AddScoped<IObjectExpander<IEnumerable<string>>, DeelZakenExpander>();
        services.AddScoped<IObjectExpander<IEnumerable<string>>, RelevanteAndereZakenExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakInformatieObjectenExpander>();
        services.AddScoped<IObjectExpander<string>, EigenschappenExpander>();
        services.AddScoped<IObjectExpander<string>, RollenExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakObjectenExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakVerzoekenExpander>();
        services.AddScoped<IObjectExpander<string>, ZaakContactmomentenExpander>();

        // Note: important to be registered as scoped. So cached within the context of one request (with expand). So this solves the n+1 problem🙂
        services.AddScoped<IGenericCache<ZaakTypeResponseDto>, GenericCache<ZaakTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
        services.AddScoped<IGenericCache<ZaakStatusResponseDto>, GenericCache<ZaakStatusResponseDto>>();
        services.AddScoped<IGenericCache<StatusTypeResponseDto>, GenericCache<StatusTypeResponseDto>>();
        services.AddScoped<IGenericCache<RolTypeResponseDto>, GenericCache<RolTypeResponseDto>>();
        services.AddScoped<
            IGenericCache<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>,
            GenericCache<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>
        >();
        services.AddScoped<IGenericCache<ResultaatTypeResponseDto>, GenericCache<ResultaatTypeResponseDto>>();
        services.AddScoped<IGenericCache<object>, GenericCache<object>>(); // Note: DRC response containing dynamically created expands
    }
}
