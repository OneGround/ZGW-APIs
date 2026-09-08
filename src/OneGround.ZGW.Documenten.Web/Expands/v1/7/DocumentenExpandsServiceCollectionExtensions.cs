using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public static partial class DocumentenExpandsServiceCollectionExtensions
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
        services.AddScoped<
            IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>,
            EnkelvoudigInformatieObject_InformatieObjectType_Catalogus_Resolver
        >();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        //
        // 2. Registratie van expand resolvers en engines voor ObjectInformatieObjectResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<ObjectInformatieObjectResponseDto>
        services.AddScoped<IExpandResolver<ObjectInformatieObjectResponseDto>, ObjectInformatieObject_InformatieObject_Resolver>();
        services.AddScoped<
            IExpandResolver<ObjectInformatieObjectResponseDto>,
            ObjectInformatieObject_InformatieObject_InformatieObjectType_Resolver
        >();
        services.AddScoped<
            IExpandResolver<ObjectInformatieObjectResponseDto>,
            ObjectInformatieObject_InformatieObject_InformatieObjectType_Catalogus_Resolver
        >();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<ObjectInformatieObjectResponseDto>(
            sp.GetServices<IExpandResolver<ObjectInformatieObjectResponseDto>>()
        ));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<ObjectInformatieObjectResponseDto>(
            sp.GetServices<IExpandResolver<ObjectInformatieObjectResponseDto>>()
        ));

        //
        // 3. Registratie van expand resolvers en engines voor GebruiksRechtResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<GebruiksRechtResponseDto>
        services.AddScoped<IExpandResolver<GebruiksRechtResponseDto>, GebruiksRecht_InformatieObject_Resolver>();
        services.AddScoped<IExpandResolver<GebruiksRechtResponseDto>, GebruiksRecht_InformatieObject_InformatieObjectType_Resolver>();
        services.AddScoped<IExpandResolver<GebruiksRechtResponseDto>, GebruiksRecht_InformatieObject_InformatieObjectType_Catalogus_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<GebruiksRechtResponseDto>(sp.GetServices<IExpandResolver<GebruiksRechtResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<GebruiksRechtResponseDto>(sp.GetServices<IExpandResolver<GebruiksRechtResponseDto>>()));

        //
        // 4. Registratie van expand resolvers en engines voor VerzendingResponseDto

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<VerzendingResponseDto>
        services.AddScoped<IExpandResolver<VerzendingResponseDto>, Verzending_InformatieObject_Resolver>();
        services.AddScoped<IExpandResolver<VerzendingResponseDto>, Verzending_InformatieObject_InformatieObjectType_Resolver>();
        services.AddScoped<IExpandResolver<VerzendingResponseDto>, Verzending_InformatieObject_InformatieObjectType_Catalogus_Resolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<VerzendingResponseDto>(sp.GetServices<IExpandResolver<VerzendingResponseDto>>()));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<VerzendingResponseDto>(sp.GetServices<IExpandResolver<VerzendingResponseDto>>()));

        // Registreer cache voor expand resolvers
        services.AddScoped<IGenericCache<EnkelvoudigInformatieObjectGetResponseDto>, GenericCache<EnkelvoudigInformatieObjectGetResponseDto>>();
    }
}
