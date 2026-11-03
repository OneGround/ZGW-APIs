using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

public static partial class ExpandsServiceCollectionExtensions
{
    public static void AddCatalogiAPIExpands(this IServiceCollection services)
    {
        // Register the caches of the expand resolvers: per scope/request and per response type, shared by all resolvers of that type.
        // So a catalogus (Catalogus_Resolver) or a type (Types_ResolverBase) that several entities in a list refer to
        // is only queried once.
        services.AddScoped(typeof(IGenericCache<>), typeof(GenericCache<>));

        //
        // 1. Registration of the expand resolvers and engines for InformatieObjectTypeResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<InformatieObjectTypeResponseDto>
        services.AddScoped<IExpandResolver<InformatieObjectTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<InformatieObjectTypeResponseDto>, InformatieObjectType_ZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<InformatieObjectTypeResponseDto>, InformatieObjectType_BesluitTypen_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<InformatieObjectTypeResponseDto>(
            sp.GetServices<IExpandResolver<InformatieObjectTypeResponseDto>>()
        ));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<InformatieObjectTypeResponseDto>(
            sp.GetServices<IExpandResolver<InformatieObjectTypeResponseDto>>()
        ));

        //
        // 2. Registration of the expand resolvers and engines for ZaakTypeResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<ZaakTypeResponseDto>
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

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<ZaakTypeResponseDto>(sp.GetServices<IExpandResolver<ZaakTypeResponseDto>>()));

        //
        // 3. Registration of the expand resolvers and engines for BesluitTypeResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<BesluitTypeResponseDto>
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_ZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_InformatieObjectTypen_Resolver>();
        services.AddScoped<IExpandResolver<BesluitTypeResponseDto>, BesluitType_ResultaatTypen_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<BesluitTypeResponseDto>(sp.GetServices<IExpandResolver<BesluitTypeResponseDto>>()));

        //
        // 4. Registration of the expand resolvers and engines for CatalogusResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<CatalogusResponseDto>
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_ZaakTypen_Resolver>();
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_BesluitTypen_Resolver>();
        services.AddScoped<IExpandResolver<CatalogusResponseDto>, Catalogus_InformatieObjectTypen_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<CatalogusResponseDto>(sp.GetServices<IExpandResolver<CatalogusResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<CatalogusResponseDto>(sp.GetServices<IExpandResolver<CatalogusResponseDto>>()));

        //
        // 5. Registration of the expand resolvers and engines for EigenschapResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<EigenschapResponseDto>
        services.AddScoped<IExpandResolver<EigenschapResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<EigenschapResponseDto>, Eigenschap_ZaakType_Resolver>();
        services.AddScoped<IExpandResolver<EigenschapResponseDto>, Eigenschap_StatusType_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<EigenschapResponseDto>(sp.GetServices<IExpandResolver<EigenschapResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<EigenschapResponseDto>(sp.GetServices<IExpandResolver<EigenschapResponseDto>>()));

        //
        // 6. Registration of the expand resolvers and engines for ResultaatTypeResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<ResultaatTypeResponseDto>
        services.AddScoped<IExpandResolver<ResultaatTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<ResultaatTypeResponseDto>, ResultaatType_ZaakType_Resolver>();
        services.AddScoped<IExpandResolver<ResultaatTypeResponseDto>, ResultaatType_BesluitTypen_Resolver>();
        services.AddScoped<IExpandResolver<ResultaatTypeResponseDto>, ResultaatType_InformatieObjectTypen_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<ResultaatTypeResponseDto>(sp.GetServices<IExpandResolver<ResultaatTypeResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<ResultaatTypeResponseDto>(sp.GetServices<IExpandResolver<ResultaatTypeResponseDto>>()));

        //
        // 7. Registration of the expand resolvers and engines for RolTypeResponseDto

        // Expand resolvers: one per expand path, registered as IExpandResolver<RolTypeResponseDto>
        services.AddScoped<IExpandResolver<RolTypeResponseDto>, Catalogus_Resolver>();
        services.AddScoped<IExpandResolver<RolTypeResponseDto>, RolType_ZaakType_Resolver>();

        // Lightweight path validation, for use in the controller
        services.AddScoped(sp => new ExpandValidator<RolTypeResponseDto>(sp.GetServices<IExpandResolver<RolTypeResponseDto>>()));

        // Generic dispatcher, for use in the controller
        services.AddScoped(sp => new ExpandEngine<RolTypeResponseDto>(sp.GetServices<IExpandResolver<RolTypeResponseDto>>()));
    }
}
