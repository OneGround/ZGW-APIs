using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._3;

public static partial class ExpandsServiceCollectionExtensions
{
    public static void AddCatalogiAPIExpands(this IServiceCollection services)
    {
        // Registreer cache voor de Catalogus_Resolver: gedeeld (per scope/request) over de drie
        // registraties hieronder, zodat een lijst met veel InformatieObjectType/ZaakType/BesluitType
        // die naar dezelfde catalogus verwijzen die maar één keer opvraagt.
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();

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

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        //
        // 3. Registratie van expand resolvers en engines voor BesluitTypeResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<BesluitTypeResponseDto>
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, Catalogus_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));
    }
}
