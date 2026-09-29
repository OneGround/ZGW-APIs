using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Bouwt het schema waartegen de <c>fields</c> selectie van POST /zaken/_zoek wordt gevalideerd.
/// De geneste sub-entiteiten volgen de DTO-graaf zoals het expand-mechanisme die ondersteunt.
/// </summary>
public static class ZaakFieldsSchema
{
    public static FieldsSchema Build() =>
        new FieldsSchemaBuilder()
            .Entity<ZaakResponseDto, ZaakTypeResponseDto>("zaaktype")
            .Entity<ZaakTypeResponseDto, CatalogusResponseDto>("catalogus")
            .Entity<ZaakResponseDto, StatusResponseDto>("status")
            .Entity<StatusResponseDto, StatusTypeResponseDto>("statustype")
            .Entity<ZaakResponseDto, ResultaatResponseDto>("resultaat")
            .Entity<ResultaatResponseDto, ResultaatTypeResponseDto>("resultaattype")
            .Entity<ZaakResponseDto, RolResponseDto>("rollen")
            .Entity<RolResponseDto, RolTypeResponseDto>("roltype")
            // Note: "betrokkeneIdentificatie" is an inline, polymorphic nested object -- its shape
            // depends on the ROL's betrokkeneType (see DomainToResponseRegister's
            // CreateRolResponseDto), so plain reflection off RolResponseDto (which doesn't declare
            // it at all) can never find it. Register every possible variant explicitly, one call per
            // type, exactly as FieldsSchemaBuilder.NestedObject's own doc comment prescribes for this
            // case; the validator then checks a requested sub-field against the union of all of them.
            .NestedObject<RolResponseDto, NatuurlijkPersoonZaakRolDto>("betrokkeneIdentificatie")
            .NestedObject<RolResponseDto, NietNatuurlijkPersoonZaakRolDto>("betrokkeneIdentificatie")
            // Note: fully-qualified -- VestigingRolResponseDto.BetrokkeneIdentificatie is typed as
            // v1._5's VestigingZaakRolDto (it carries kvknummer; the v1/base one doesn't), and this
            // file's "using ...Contracts.v1;" would otherwise resolve the unqualified name to the
            // wrong (v1) type, silently rejecting a real field ("kvknummer") as unknown.
            .NestedObject<RolResponseDto, Zaken.Contracts.v1._5.VestigingZaakRolDto>("betrokkeneIdentificatie")
            .NestedObject<RolResponseDto, OrganisatorischeEenheidZaakRolDto>("betrokkeneIdentificatie")
            .NestedObject<RolResponseDto, MedewerkerZaakRolDto>("betrokkeneIdentificatie")
            .Build();
}
