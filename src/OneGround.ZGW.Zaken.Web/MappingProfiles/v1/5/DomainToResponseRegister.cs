using System;
using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._5;

// Note: This Register adds extended mappings (above the ones defined in v1.0 and v1.2). config.Scan discovers
// both this register and MappingProfiles.v1.DomainToResponseRegister in the same TypeAdapterConfig at startup,
// so shared nested-DTO configs registered over there (e.g. AdresZaakObject->AdresZaakObjectDto,
// NatuurlijkPersoonZaakRol->NatuurlijkPersoonZaakRolDto, the Geometry->Geometry / JToken->JToken same-type
// clone rules, etc.) apply here too - this file only registers the type pairs that are genuinely new or
// different in v1.5 (distinct v1._5-namespaced DTOs), exactly mirroring which CreateMap calls the source
// AutoMapper profile (DomainToResponseProfile.cs, same folder) declares.
public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        //
        // 1. Zaak

        config
            .NewConfig<Zaak, ZaakResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Deelzaken, src => MapsterUrlResolver.ResolveUrls(src.Deelzaken))
            .Map(dest => dest.Hoofdzaak, src => MapsterUrlResolver.ResolveUrl(src.Hoofdzaak))
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDate(src.Registratiedatum))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDate(src.Startdatum))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDate(src.Einddatum))
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.StringDateFromDate(src.EinddatumGepland))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.StringDateFromDate(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.Publicatiedatum, src => ProfileHelper.StringDateFromDate(src.Publicatiedatum))
            .Map(dest => dest.LaatsteBetaaldatum, src => ProfileHelper.StringDateFromDateTime(src.LaatsteBetaaldatum, true))
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum))
            .Map(dest => dest.Eigenschappen, src => MapsterUrlResolver.ResolveUrls(src.ZaakEigenschappen))
            .Map(dest => dest.Resultaat, src => MapsterUrlResolver.ResolveUrl(src.Resultaat))
            .Map(dest => dest.Rollen, src => MapsterUrlResolver.ResolveUrls(src.ZaakRollen))
            .Map(dest => dest.ZaakInformatieObjecten, src => MapsterUrlResolver.ResolveUrls(src.ZaakInformatieObjecten))
            .Map(dest => dest.ZaakObjecten, src => MapsterUrlResolver.ResolveUrls(src.ZaakObjecten))
            // Note: dest.Status is a plain string (scalar), not a collection, so the global
            // EmptyCollectionIfNull destination transform does not apply here. This reproduces the
            // AutoMapper PreCondition explicitly: a null ZaakStatussen navigation folds to null,
            // otherwise the latest (by DatumStatusGezet) status's URL is resolved. Same fold as the v1
            // sibling register's identical Zaak->ZaakResponseDto config.
            .Map(
                dest => dest.Status,
                src =>
                    src.ZaakStatussen == null
                        ? null
                        : MapsterUrlResolver.ResolveUrl(src.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault())
            )
            .Map(dest => dest.StartdatumBewaartermijn, src => ProfileHelper.StringDateFromDate(src.StartdatumBewaartermijn))
            .Map(dest => dest.Toelichting, src => ProfileHelper.EmptyWhenNull(src.Toelichting))
            .Map(dest => dest.BetalingsindicatieWeergave, src => ProfileHelper.EmptyWhenNull(src.BetalingsindicatieWeergave))
            .Map(dest => dest.OpdrachtgevendeOrganisatie, src => ProfileHelper.EmptyWhenNull(src.OpdrachtgevendeOrganisatie))
            .Map(dest => dest.Processobjectaard, src => ProfileHelper.EmptyWhenNull(src.Processobjectaard));

        // Note: The source AutoMapper profile registers CreateMap<ZaakProcessobject, ZaakProcessobjectDto>()
        // twice (once empty as a no-op placeholder, once with the real explicit member mappings below) -
        // AutoMapper merges both onto the same TypePair, with the second (member-explicit) registration being
        // the one that actually matters. Mapster only needs the single meaningful registration here.
        config
            .NewConfig<ZaakProcessobject, ZaakProcessobjectDto>()
            .Map(dest => dest.Datumkenmerk, src => src.Datumkenmerk)
            .Map(dest => dest.Identificatie, src => src.Identificatie)
            .Map(dest => dest.Objecttype, src => src.Objecttype)
            .Map(dest => dest.Registratie, src => src.Registratie);

        // Note: This map is used to merge an existing ZAAK with the PATCH operation
        config
            .NewConfig<Zaak, ZaakRequestDto>()
            .Map(dest => dest.Hoofdzaak, src => MapsterUrlResolver.ResolveUrl(src.Hoofdzaak))
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDate(src.Registratiedatum))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDate(src.Startdatum))
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.StringDateFromDate(src.EinddatumGepland))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.StringDateFromDate(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.Publicatiedatum, src => ProfileHelper.StringDateFromDate(src.Publicatiedatum))
            .Map(dest => dest.LaatsteBetaaldatum, src => ProfileHelper.StringDateFromDateTime(src.LaatsteBetaaldatum, true))
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum))
            .Map(dest => dest.OpdrachtgevendeOrganisatie, src => src.OpdrachtgevendeOrganisatie)
            .Map(dest => dest.Processobjectaard, src => src.Processobjectaard)
            .Map(dest => dest.StartdatumBewaartermijn, src => ProfileHelper.StringDateFromDate(src.StartdatumBewaartermijn));

        //
        // 2. ZaakStatus

        config
            .NewConfig<ZaakStatus, ZaakStatusCreateResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.DatumStatusGezet, src => ProfileHelper.StringDateFromDateTime(src.DatumStatusGezet, true));

        config
            .NewConfig<ZaakStatus, ZaakStatusGetResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.DatumStatusGezet, src => ProfileHelper.StringDateFromDateTime(src.DatumStatusGezet, true))
            .Map(dest => dest.ZaakInformatieObjecten, src => MapsterUrlResolver.ResolveUrls(src.Zaak.ZaakInformatieObjecten));

        //
        // 4. ZaakObjecten

        config
            .NewConfig<ZaakObject, ZaakObjectResponseDto>()
            .ConstructUsing(src => CreateZaakObjectResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in >= v1.2
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        // Note: This map is used to merge an existing ZAAKOBJECT with the PATCH operation
        config
            .NewConfig<ZaakObject, ZaakObjectRequestDto>()
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        // Note: The 8 ObjectIdentificatie assignments below adapt into the v1-namespaced XxxZaakObjectDto
        // types (e.g. Zaken.Contracts.v1.AdresZaakObjectDto) - confirmed from the DTOs' own source: e.g.
        // AdresZaakObjectRequestDto.ObjectIdentificatie is typed as (unqualified, in-file) AdresZaakObjectDto,
        // which resolves to OneGround.ZGW.Zaken.Contracts.v1.AdresZaakObjectDto because that root v1 namespace
        // is a structurally enclosing namespace of v1._5.Requests.ZaakObject (no local v1.5-specific override
        // exists for these object-identity DTOs, unlike VestigingZaakRolDto). Those AdresZaakObject->
        // Zaken.Contracts.v1.AdresZaakObjectDto (etc.) nested maps are registered in the ALREADY-MERGED
        // v1/DomainToResponseRegister.cs, not here - passing `config` explicitly to Adapt keeps the nested
        // map resolving against this shared local config (populated by config.Scan in production, and by both
        // registers' tests explicitly) rather than Mapster's ambient TypeAdapterConfig.GlobalSettings.
        config
            .NewConfig<AdresZaakObject, AdresZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.AdresZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<BuurtZaakObject, BuurtZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.BuurtZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<GemeenteZaakObject, GemeenteZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.GemeenteZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<OverigeZaakObject, OverigeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.OverigeZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<PandZaakObject, PandZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.PandZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<WozWaardeZaakObject, WozWaardeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<Zaken.Contracts.v1.WozWaardeZaakObjectDto>(config))
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        //
        // 5. ZaakInformatieObjecten

        config
            .NewConfig<ZaakInformatieObject, ZaakInformatieObjectResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.AardRelatieWeergave, src => AardRelatieWeergaveToString(src.AardRelatieWeergave))
            .Map(dest => dest.RegistratieDatum, src => ProfileHelper.StringDateFromDateTime(src.RegistratieDatum, true))
            .Map(dest => dest.VernietigingsDatum, src => ProfileHelper.StringDateFromDateTime(src.VernietigingsDatum, true))
            .Map(dest => dest.Status, src => MapsterUrlResolver.ResolveUrl(src.Status));

        // Note: This map is used to merge an existing ZAAKINFORMATIEOBJECT with the PATCH operation
        config
            .NewConfig<ZaakInformatieObject, ZaakInformatieObjectRequestDto>()
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.Status, src => MapsterUrlResolver.ResolveUrl(src.Status));

        //
        // 6. ZaakRol

        config
            .NewConfig<ZaakRol, ZaakRolResponseDto>()
            .ConstructUsing(src => CreateZaakRolResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.IndicatieMachtiging, src => !src.IndicatieMachtiging.HasValue ? "" : src.IndicatieMachtiging.ToString())
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDateTime(src.Registratiedatum, true))
            // Note: Map only zaak-statussen which matches zaak-status.GezetDoor. dest.Statussen
            // (IEnumerable<string>, confirmed to have NO field initializer, so it defaults to null) goes
            // through a plain .Map(...), not the Risk #17 .Ignore()+.AfterMapping treatment - this is NOT a
            // PreCondition (which bypasses AutoMapper's member assignment/null-substitution entirely); it's a
            // MapFrom whose lambda body itself computes null via an inline ternary. See the explicit null-check
            // below reproducing that fold. Whether AutoMapper's own real output actually differs (empty vs
            // null) for a null ZaakStatussen is intentionally re-verified in this file's own tests and reported
            // to the orchestrator, since an explicit MapFrom returning null still goes through AutoMapper's
            // normal AllowNullCollections=false null-substitution for collection members - a subtlety this
            // register's own null-check cannot itself resolve.
            .Map(
                dest => dest.Statussen,
                src =>
                    src.Zaak.ZaakStatussen == null
                        ? null
                        : MapsterUrlResolver.ResolveUrls(
                            src.Zaak.ZaakStatussen.Where(s => s.GezetDoor == src.Betrokkene).OrderBy(s => s.DatumStatusGezet)
                        )
            );

        config.NewConfig<ContactpersoonRol, ContactpersoonRolDto>();

        // Note: We cannot use the v1.0 mapper because VestigingZaakRolDto contains a new field KvkNummer
        config.NewConfig<VestigingZaakRol, VestigingZaakRolDto>();

        //
        // 11. ZaakVerzoek

        config
            .NewConfig<ZaakVerzoek, ZaakVerzoekResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        //
        // 12. ZaakContactmoment

        config
            .NewConfig<ZaakContactmoment, ZaakContactmomentResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));
    }

    // Note: The two factories below are Shape-B ConstructUsing configs (no ForAllMembers(Ignore) / no blanket
    // .MapWith) - the surrounding NewConfig's own .Map/.Ignore rules (Url/Uuid/Zaak/etc.) still apply on top of
    // whatever this factory returns. Both factory bodies recursively adapt a nested source into a nested
    // destination type; `config` is threaded through explicitly and used via `.Adapt<T>(config)` so the
    // recursive call resolves against THIS shared local config rather than Mapster's ambient
    // TypeAdapterConfig.GlobalSettings. Most target types below are the v1-namespaced DTOs (e.g.
    // Zaken.Contracts.v1.AdresZaakObjectDto), registered by the ALREADY-MERGED v1/DomainToResponseRegister.cs,
    // not by this file - `.Adapt<T>(config)` on a null source returns null (Mapster's own null-source
    // handling), matching AutoMapper's context.Mapper.Map<T>(null) behavior.

    private static ZaakRolResponseDto CreateZaakRolResponseDto(ZaakRol source, TypeAdapterConfig config)
    {
        // when corresponding relation i.e. source.NatuurlijkPersoon == null, we return base ZaakRolResponseDto,
        // because we don't need to include BetrokkeneIdentificatie in GetAll requests
        return source.BetrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => new NatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.NatuurlijkPersoon.Adapt<Zaken.Contracts.v1.NatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.niet_natuurlijk_persoon => new NietNatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.NietNatuurlijkPersoon.Adapt<Zaken.Contracts.v1.NietNatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.vestiging => new VestigingZaakRolResponseDto
            {
                // Note: VestigingZaakRolDto contains one new field KvKNummer so it uses not the v1 Dto here -
                // this is the v1.5-LOCAL VestigingZaakRolDto (registered by this same file, above), so it does
                // not depend on the v1 register being registered alongside this one.
                BetrokkeneIdentificatie = source.Vestiging.Adapt<VestigingZaakRolDto>(config),
            },
            BetrokkeneType.organisatorische_eenheid => new OrganisatorischeEenheidZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.OrganisatorischeEenheid.Adapt<Zaken.Contracts.v1.OrganisatorischeEenheidZaakRolDto>(config),
            },
            BetrokkeneType.medewerker => new MedewerkerZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.Medewerker.Adapt<Zaken.Contracts.v1.MedewerkerZaakRolDto>(config),
            },
            _ => new ZaakRolResponseDto(),
        };
    }

    private static ZaakObjectResponseDto CreateZaakObjectResponseDto(ZaakObject source, TypeAdapterConfig config)
    {
        return source.ObjectType switch
        {
            ObjectType.adres => new AdresZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Adres.Adapt<Zaken.Contracts.v1.AdresZaakObjectDto>(config),
            },
            ObjectType.buurt => new BuurtZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Buurt.Adapt<Zaken.Contracts.v1.BuurtZaakObjectDto>(config),
            },
            ObjectType.pand => new PandZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Pand.Adapt<Zaken.Contracts.v1.PandZaakObjectDto>(config),
            },
            ObjectType.kadastrale_onroerende_zaak => new KadastraleOnroerendeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.KadastraleOnroerendeZaak.Adapt<Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto>(config),
            },
            ObjectType.gemeente => new GemeenteZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Gemeente.Adapt<Zaken.Contracts.v1.GemeenteZaakObjectDto>(config),
            },
            ObjectType.terrein_gebouwd_object => new TerreinGebouwdObjectZaakObjectResponseDto
            {
                ObjectIdentificatie = source.TerreinGebouwdObject.Adapt<Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto>(config),
            },
            ObjectType.overige => new OverigeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Overige.Adapt<Zaken.Contracts.v1.OverigeZaakObjectDto>(config),
            },
            ObjectType.woz_waarde => new WozWaardeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.WozWaardeObject.Adapt<Zaken.Contracts.v1.WozWaardeZaakObjectDto>(config),
            },
            ObjectType.besluit => new ZaakObjectResponseDto(),
            ObjectType.status => new ZaakObjectResponseDto(),
            ObjectType.enkelvoudig_document => new ZaakObjectResponseDto(),

            // decision was made to implement other types later on
            //_ => throw new NotImplementedException($"{source.ObjectType} is not yet implemented."),
            _ => new ZaakObjectResponseDto(),
        };
    }

    private static string AardRelatieWeergaveToString(AardRelatieWeergave aardRelatieWeergave)
    {
        return aardRelatieWeergave switch
        {
            AardRelatieWeergave.hoort_bij_omgekeerd_kent => "Hoort bij, omgekeerd: kent",
            AardRelatieWeergave.legt_vast_omgekeerd_kan_vastgelegd_zijn_als => "Legt vast, omgekeerd: kan vastgelegd zijn als",
            _ => throw new InvalidOperationException($"{aardRelatieWeergave} not handled."),
        };
    }
}
