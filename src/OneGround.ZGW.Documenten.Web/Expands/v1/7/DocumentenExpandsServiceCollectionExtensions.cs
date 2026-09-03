using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public static class DocumentenExpandsServiceCollectionExtensions
{
    public static void AddDocumentenAPIFieldsSelection(this IServiceCollection services)
    {
        services.AddScoped(sp =>
        {
            var expandablePaths = sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
                .SelectMany(r => r.AdditionalPaths.Select(a => a.Path).Prepend(r.Path));

            return new FieldsValidator<EnkelvoudigInformatieObjectGetResponseDto>(DocumentFieldsSchema.Build(), expandablePaths);
        });
    }

    public static void AddDocumentenAPIExpands(this IServiceCollection services)
    {
        //
        // Registratie van expand resolvers en engines voor ZaakResponse

        // Expand resolvers — één per expand-pad, geregistreerd als IExpandResolver<ZaakResponse>
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>, InformatieObjectTypeResolver>();
        services.AddScoped<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>, InformatieObjectTypeCatalogusResolver>();

        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, StatusResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, StatusStatustypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, ResultaatResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, ResultaatResultaattypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, ZaakinformatieobjectenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, ZaakinformatieobjectInformatieobjectResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, ZaakinformatieobjectInformatieobjectTypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, RollenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, RollenRoltypeResolver>();
        //// Hoofdzaak resolver + expliciete resolvers voor alle "hoofdzaak.*" expand-paden
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakZaaktypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakZaaktypeCatalogusResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakStatusResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakStatusStatustypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakResultaatResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakResultaatResultaattypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakZaakinformatieobjectenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakZaakinformatieobjectInformatieobjectResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakRollenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, HoofdzaakRollenRoltypeResolver>();
        //// Deelzaken resolvers — mutate-in-place patroon (lijst is niet IExpandable)
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenZaaktypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenZaaktypeCatalogusResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenStatusResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenStatusStatustypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenResultaatResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenResultaatResultaattypeResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenZaakinformatieobjectenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenZaakinformatieobjectInformatieobjectResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenRollenResolver>();
        //services.AddSingleton<IExpandResolver<ZaakResponseDto>, DeelzakenRollenRoltypeResolver>();

        // Lichtgewicht pad-validatie voor gebruik in de controller
        services.AddScoped(sp => new ExpandValidator<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        // Generieke dispatcher voor gebruik in de handlers
        services.AddScoped(sp => new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>(
            sp.GetServices<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>()
        ));

        //
        // Registratie van expand resolvers en engines voor StatusResponse

        //services.AddSingleton<IExpandResolver<StatusResponseDto>, StatusZaakResolver>();
        //services.AddSingleton<IExpandResolver<StatusResponseDto>, StatusStatustypeForStatusResolver>();
        //services.AddSingleton<IExpandResolver<StatusResponseDto>, StatusZaakinformatieobjectenResolver>();
        //services.AddSingleton<IExpandResolver<StatusResponseDto>, StatusZaakinformatieobjectInformatieobjectResolver>();

        //// Lichtgewicht pad-validatie voor gebruik in de controller
        //services.AddSingleton(sp => new ExpandValidator<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));

        //// Generieke dispatcher voor gebruik in de handlers
        //services.AddSingleton(sp => new ExpandEngine<StatusResponseDto>(sp.GetServices<IExpandResolver<StatusResponseDto>>()));
    }

    /// <summary>
    /// Bouwt het schema waartegen de <c>fields</c> selectie van POST /zaken/_zoek wordt gevalideerd.
    /// De geneste sub-entiteiten volgen de DTO-graaf zoals het expand-mechanisme die ondersteunt.
    /// De toegestane scalaire velden per niveau worden automatisch afgeleid uit de <c>JsonPropertyName</c>
    /// attributen van de betreffende response-DTO's.
    /// </summary>
    public static class DocumentFieldsSchema
    {
        public static FieldsSchema Build() =>
            new FieldsSchemaBuilder()
                // Zaak → sub-entiteiten. hoofdzaak en deelzaken zijn zelf weer een Zaak (recursief).
                .Entity<EnkelvoudigInformatieObjectGetResponseDto, InformatieObjectTypeResponseDto>("informatieobjecttype")
                //.Entity<ZaakResponseDto, StatusResponseDto>("status")
                //.Entity<ZaakResponseDto, ResultaatResponseDto>("resultaat")
                //.Entity<ZaakResponseDto, ZaakResponseDto>("hoofdzaak")
                //.Entity<ZaakResponseDto, ZaakResponseDto>("deelzaken")
                //.Entity<ZaakResponseDto, ZaakInformatieObjectResponseDto>("zaakinformatieobjecten")
                //.Entity<ZaakResponseDto, RolResponseDto>("rollen")
                // Diepere niveaus.
                .Entity<InformatieObjectTypeResponseDto, CatalogusResponseDto>("catalogus")
                //.Entity<StatusResponseDto, StatusTypeResponseDto>("statustype")
                //.Entity<ResultaatResponseDto, ResultaatTypeResponseDto>("resultaattype")
                //.Entity<ZaakInformatieObjectResponseDto, InformatieObjectResponseDto>("informatieobject")
                //.Entity<InformatieObjectResponseDto, InformatieObjectTypeResponseDto>("informatieobjecttype")
                //.Entity<RolResponseDto, RolTypeResponseDto>("roltype")
                // Polymorfe inline objecten: registreer elke variant zodat inner field-selectie tegen de
                // unie van hun velden gevalideerd wordt. Concreet getypeerde geneste objecten (verlenging,
                // opschorting, verblijfsadres, kenmerken, ...) worden via reflectie afgeleid — geen registratie.
                //.NestedObject<RolResponseDto, RolMedewerkerDto>("betrokkeneIdentificatie")
                //.NestedObject<RolResponseDto, RolNatuurlijkPersoonDto>("betrokkeneIdentificatie")
                //.NestedObject<RolResponseDto, RolNietNatuurlijkPersoonDto>("betrokkeneIdentificatie")
                //.NestedObject<RolResponseDto, RolOrganisatorischeEenheidDto>("betrokkeneIdentificatie")
                //.NestedObject<RolResponseDto, RolVestigingDto>("betrokkeneIdentificatie")
                .Build();
    }
}
