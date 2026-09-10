using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public static partial class ExpandsServiceCollectionExtensions
{
    public static void AddDocumentenAPIFieldsValidators(this IServiceCollection services)
    {
        services.AddScoped(sp =>
        {
            var expandablePaths = sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
                .SelectMany(r => r.AdditionalPaths.Select(a => a.Path).Prepend(r.Path));

            return new FieldsValidator<EnkelvoudigInformatieObjectGetResponseDto>(EnkelvoudigInformatieObjectFieldsSchema.Build(), expandablePaths);
        });
    }

    public static void AddDocumentenAPIExpands(this IServiceCollection services)
    {
        //
        // 1. Registratie van expand resolvers en engines voor EnkelvoudigInformatieObjectGetResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>, EnkelvoudigInformatieObject_InformatieObjectType_Resolver>();
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>(
            sp => new InformatieObjectTypeCatalogusResolver<EnkelvoudigInformatieObjectGetResponseDto>(
                sp.GetRequiredService<ICatalogiServiceAgentDecorator>(),
                sp.GetRequiredService<IGenericCache<CatalogusResponseDto>>(),
                path: "informatieobjecttype.catalogus",
                parent: "informatieobjecttype"
            )
        );

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        //
        // 2. Registratie van expand resolvers en engines voor ObjectInformatieObjectResponseDto, GebruiksRechtResponseDto en VerzendingResponseDto
        // Deze drie hebben identieke resolver/validator/engine-registraties (alleen TEntity en de
        // informatieobject-URL verschillen), vandaar de gedeelde helper hieronder.
        AddInformatieObjectLinkedExpandSet<ObjectInformatieObjectResponseDto>(services, e => e.InformatieObject);
        AddInformatieObjectLinkedExpandSet<GebruiksRechtResponseDto>(services, e => e.InformatieObject);
        AddInformatieObjectLinkedExpandSet<VerzendingResponseDto>(services, e => e.InformatieObject);

        // Registreer cache voor expand resolvers
        // Note: Deze registraties staan ook (nog) in de [Obsolete] v1.5 AddExpandables() -- v1.7 moet
        // hier niet stilzwijgend op leunen, anders breekt v1.7 expand zodra AddExpandables() ooit
        // verwijderd wordt (zie de TODO bij de aanroep in Startup.cs).
        services.AddScoped<IGenericCache<EnkelvoudigInformatieObjectGetResponseDto>, GenericCache<EnkelvoudigInformatieObjectGetResponseDto>>();
        services.AddScoped<IGenericCache<InformatieObjectTypeResponseDto>, GenericCache<InformatieObjectTypeResponseDto>>();
        services.AddScoped<IGenericCache<CatalogusResponseDto>, GenericCache<CatalogusResponseDto>>();
    }

    /// <summary>
    /// Registreert de resolvers/validator/engine voor een entity die "informatieobject" (en de
    /// nesting daarvan: informatieobjecttype, informatieobjecttype.catalogus) kan expanden.
    /// Gedeeld door ObjectInformatieObject, GebruiksRecht en Verzending, waarvan de registratie
    /// verder identiek is -- alleen de URL-selector verschilt.
    /// </summary>
    private static void AddInformatieObjectLinkedExpandSet<TEntity>(IServiceCollection services, Func<TEntity, string> informatieObjectUrl)
        where TEntity : IExpandable
    {
        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<TEntity>
        services.AddScoped<IExpandResolver<TEntity>>(sp => new InformatieObjectResolver<TEntity>(
            sp,
            sp.GetRequiredService<IEntityUriService>(),
            informatieObjectUrl
        ));
        services.AddScoped<IExpandResolver<TEntity>, InformatieObjectTypeResolver<TEntity>>();
        services.AddScoped<IExpandResolver<TEntity>>(sp => new InformatieObjectTypeCatalogusResolver<TEntity>(
            sp.GetRequiredService<ICatalogiServiceAgentDecorator>(),
            sp.GetRequiredService<IGenericCache<CatalogusResponseDto>>(),
            path: "informatieobject.informatieobjecttype.catalogus",
            parent: "informatieobject.informatieobjecttype"
        ));

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<TEntity>(sp.GetServices<IExpandResolver<TEntity>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<TEntity>(sp.GetServices<IExpandResolver<TEntity>>()));
    }
}
