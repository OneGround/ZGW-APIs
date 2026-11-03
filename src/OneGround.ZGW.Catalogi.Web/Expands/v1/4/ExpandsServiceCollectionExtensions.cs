using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

public static partial class ExpandsServiceCollectionExtensions
{
    public static void AddCatalogiAPIExpands(this IServiceCollection services)
    {
        // Registreer de caches van de expand resolvers: per scope/request en per response-type, gedeeld door alle resolvers van dat type.
        // Zo wordt een catalogus (Catalogus_Resolver) of een type (ChildTypen_ResolverBase) waar meerdere entities in een lijst naar verwijzen
        // maar één keer opgevraagd.
        services.AddScoped(typeof(IGenericCache<>), typeof(GenericCache<>));

        //
        // 1. Registratie van expand resolvers en engines voor InformatieObjectTypeResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<InformatieObjectTypeResponseDto>
        services.AddScoped<IExpandResolver<InformatieObjectTypeResponseDto>, Catalogus_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<InformatieObjectTypeResponseDto>(
            sp.GetServices<IExpandResolver<InformatieObjectTypeResponseDto>>()
        ));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<InformatieObjectTypeResponseDto>(
            sp.GetServices<IExpandResolver<InformatieObjectTypeResponseDto>>()
        ));

        //
        // 2. Registratie van expand resolvers en engines voor ZaakTypeResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<ZaakTypeResponseDto>
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_StatusTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_ResultaatTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_Eigenschappen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_RolTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_ZaakObjectTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_BesluitTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_InformatieObjectTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_DeelZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<ZaakTypeResponseDto>, ZaakType_GerelateerdeZaakTypen_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        //
        // 3. Registratie van expand resolvers en engines voor BesluitTypeResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<BesluitTypeResponseDto>
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_ZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_InformatieObjectTypen_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_ResultaatTypen_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));

        //
        // 4. Registratie van expand resolvers en engines voor CatalogusResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<CatalogusResponseDto>
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_ZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_BesluitTypen_Resolver>();
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_InformatieObjectTypen_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<CatalogusResponseDto>(sp.GetServices<IExpandResolver<CatalogusResponseDto>>()));

        // Generieke dispatcher voor gebruik in de controller
        services.AddScoped(sp => new ExpandEngine<CatalogusResponseDto>(sp.GetServices<IExpandResolver<CatalogusResponseDto>>()));
    }
}
