using System;
using System.Linq;
using Mapster;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1._2;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // NetTopologySuite's Geometry is abstract with no parameterless constructor, so Mapster can't build
        // its usual clone expression for same-type Geometry->Geometry members (Zaak.Zaakgeometrie, mapped
        // below on both Zaak->ZaakResponseDto and Zaak->ZaakRequestDto). AutoMapper falls back to a direct
        // reference copy for identical source/destination types; this reproduces that. Also registered by
        // RequestToDomainRegister (for the reverse direction) - re-registering the identical rule here is
        // harmless (same TypePair, same behavior) and keeps this register self-sufficient for what it maps,
        // independent of config.Scan discovery order.
        config.NewConfig<Geometry, Geometry>().MapWith(src => src);

        // Same class of problem as Geometry above: OverigeZaakObject->OverigeZaakObjectDto (further down)
        // assigns dest.OverigeData (a JToken) via JToken.Parse(...) - a same-type JToken->JToken result.
        // JToken is abstract with no accessible parameterless constructor, so Mapster's default same-type
        // clone expression fails to compile unless told to just use the value as-is.
        config.NewConfig<JToken, JToken>().MapWith(src => src);

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
            // Note: dest.Status is a plain string (scalar), not a collection, so the global
            // EmptyCollectionIfNull destination transform does not apply here. This reproduces the
            // AutoMapper PreCondition explicitly: a null ZaakStatussen navigation folds to null,
            // otherwise the latest (by DatumStatusGezet) status's URL is resolved.
            .Map(
                dest => dest.Status,
                src =>
                    src.ZaakStatussen == null
                        ? null
                        : MapsterUrlResolver.ResolveUrl(src.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault())
            )
            .Map(dest => dest.Toelichting, src => ProfileHelper.EmptyWhenNull(src.Toelichting))
            .Map(dest => dest.BetalingsindicatieWeergave, src => ProfileHelper.EmptyWhenNull(src.BetalingsindicatieWeergave))
            // Note: Betalingsindicatie/Vertrouwelijkheidaanduiding differ from their source's casing
            // (BetalingsIndicatie/VertrouwelijkheidAanduiding) -- reproduced automatically in production
            // only via the global NameMatchingStrategy.IgnoreCase default, which a bare TypeAdapterConfig()
            // (as used by this register's own unit tests) doesn't have. Explicit .Map(...) calls make the
            // register correct under both a bare test config and the real seam, mirroring the identical
            // note on RequestToDomainRegister's ZaakRequestDto->Zaak config for the reverse direction.
            .Map(dest => dest.Betalingsindicatie, src => src.BetalingsIndicatie.ToString())
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => src.VertrouwelijkheidAanduiding.ToString());

        config.NewConfig<RelevanteAndereZaak, RelevanteAndereZaakDto>();
        config.NewConfig<ZaakKenmerk, ZaakKenmerkDto>();
        config.NewConfig<ZaakVerlenging, ZaakVerlengingDto>();
        config.NewConfig<ZaakOpschorting, ZaakOpschortingDto>();

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
            // See identical note on the Zaak->ZaakResponseDto config above.
            .Map(dest => dest.Betalingsindicatie, src => src.BetalingsIndicatie.ToString())
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => src.VertrouwelijkheidAanduiding.ToString());

        //
        // 2. ZaakStatus

        config
            .NewConfig<ZaakStatus, ZaakStatusResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.DatumStatusGezet, src => ProfileHelper.StringDateFromDateTime(src.DatumStatusGezet, true));

        //
        // 3. ZaakEigenschap

        config
            .NewConfig<ZaakEigenschap, ZaakEigenschapResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        //
        // 4. ZaakObjecten

        config
            .NewConfig<ObjectTypeOverigeDefinitie, ObjectTypeOverigeDefinitieDto>() // Note: Supported in v1.2 only
            .Map(dest => dest.Url, src => src.Url)
            .Map(dest => dest.Schema, src => src.Schema)
            .Map(dest => dest.ObjectData, src => src.ObjectData);

        config
            .NewConfig<ZaakObject, ZaakObjectResponseDto>()
            .ConstructUsing(src => CreateZaakObjectResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Ignore(dest => dest.Version);

        config.NewConfig<AdresZaakObject, AdresZaakObjectDto>();
        config.NewConfig<BuurtZaakObject, BuurtZaakObjectDto>();
        config.NewConfig<PandZaakObject, PandZaakObjectDto>();
        config.NewConfig<GemeenteZaakObject, GemeenteZaakObjectDto>();
        config.NewConfig<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectDto>();
        config
            .NewConfig<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectDto>()
            .MapWith(s => new TerreinGebouwdObjectZaakObjectDto
            {
                Identificatie = s.Identificatie,
                AdresAanduidingGrp = s.IsAdresAanduidingGrp
                    ? new AdresAanduidingGrpDto
                    {
                        AoaHuisletter = s.AdresAanduidingGrp_AoaHuisletter,
                        AoaHuisnummer = s.AdresAanduidingGrp_AoaHuisnummer,
                        AoaHuisnummertoevoeging = s.AdresAanduidingGrp_AoaHuisnummertoevoeging,
                        AoaPostcode = s.AdresAanduidingGrp_AoaPostcode,
                        GorOpenbareRuimteNaam = s.AdresAanduidingGrp_GorOpenbareRuimteNaam,
                        NumIdentificatie = s.AdresAanduidingGrp_NumIdentificatie,
                        OaoIdentificatie = s.AdresAanduidingGrp_OaoIdentificatie,
                        OgoLocatieAanduiding = s.AdresAanduidingGrp_OgoLocatieAanduiding,
                        WplWoonplaatsNaam = s.AdresAanduidingGrp_WplWoonplaatsNaam,
                    }
                    : null,
            });

        config.NewConfig<OverigeZaakObject, OverigeZaakObjectDto>().Map(dest => dest.OverigeData, src => JToken.Parse(src.OverigeData));

        config.NewConfig<AanduidingWozObject, AanduidingWozObjectDto>();
        config.NewConfig<WozObject, WozObjectDto>();
        config.NewConfig<WozWaardeZaakObject, WozWaardeZaakObjectDto>();

        // Note: This maps is used to merge an existing ObjectTypeOverigeDefinitie with the PATCH operation
        config
            .NewConfig<ObjectTypeOverigeDefinitieDto, ObjectTypeOverigeDefinitie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject);

        config
            .NewConfig<ZaakObject, ZaakObjectRequestDto>()
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Ignore(dest => dest.Version);

        // Note: The 8 ObjectIdentificatie assignments below are AutoMapper implicit nested maps
        // (source and destination member types differ - e.g. AdresZaakObject -> AdresZaakObjectDto -
        // and AutoMapper auto-resolves the map registered elsewhere in this file). A bare
        // `.Map(dest => dest.ObjectIdentificatie, src => src)` would either not compile (type
        // mismatch) or, if it did via some implicit path, would resolve against Mapster's ambient
        // TypeAdapterConfig.GlobalSettings instead of this local config, silently ignoring any
        // custom rule registered above for that nested type pair. Passing `config` explicitly to
        // Adapt keeps the nested map on this local config.
        config
            .NewConfig<AdresZaakObject, AdresZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<AdresZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<BuurtZaakObject, BuurtZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<BuurtZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<GemeenteZaakObject, GemeenteZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<GemeenteZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<KadastraleOnroerendeZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<OverigeZaakObject, OverigeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<OverigeZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<PandZaakObject, PandZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<PandZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<TerreinGebouwdObjectZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.Object)
            .Ignore(dest => dest.ObjectType)
            .Ignore(dest => dest.ObjectTypeOverigeDefinitie)
            .Ignore(dest => dest.ObjectTypeOverige)
            .Ignore(dest => dest.RelatieOmschrijving);

        config
            .NewConfig<WozWaardeZaakObject, WozWaardeZaakObjectRequestDto>()
            .Map(dest => dest.ObjectIdentificatie, src => src.Adapt<WozWaardeZaakObjectDto>(config))
            .Ignore(dest => dest.Version)
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
            .Map(dest => dest.RegistratieDatum, src => ProfileHelper.StringDateFromDateTime(src.RegistratieDatum, true));

        // Note: This map is used to merge an existing ZAAKINFORMATIEOBJECT with the PATCH operation
        config
            .NewConfig<ZaakInformatieObject, ZaakInformatieObjectRequestDto>()
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        //
        // 6. ZaakRol

        config
            .NewConfig<ZaakRol, ZaakRolResponseDto>()
            .ConstructUsing(src => CreateZaakRolResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.IndicatieMachtiging, src => !src.IndicatieMachtiging.HasValue ? "" : src.IndicatieMachtiging.ToString())
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDateTime(src.Registratiedatum, true));

        config
            .NewConfig<NatuurlijkPersoonZaakRol, NatuurlijkPersoonZaakRolDto>()
            .Map(dest => dest.Geboortedatum, src => ProfileHelper.StringDateFromDateTime(src.Geboortedatum, true))
            .Map(dest => dest.InpBsn, src => src.InpBsnEncrypted);
        config.NewConfig<NietNatuurlijkPersoonZaakRol, NietNatuurlijkPersoonZaakRolDto>();
        config.NewConfig<VestigingZaakRol, VestigingZaakRolDto>();
        config.NewConfig<OrganisatorischeEenheidZaakRol, OrganisatorischeEenheidZaakRolDto>();
        config.NewConfig<MedewerkerZaakRol, MedewerkerZaakRolDto>();
        config.NewConfig<Verblijfsadres, VerblijfsadresDto>();
        config.NewConfig<SubVerblijfBuitenland, SubVerblijfBuitenlandDto>();

        //
        // 7. ZaakResultaat

        config
            .NewConfig<ZaakResultaat, ZaakResultaatResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.Uuid, src => src.Id);

        // Note: This map is used to merge an existing ZaakResultaat with the PATCH operation
        config.NewConfig<ZaakResultaat, ZaakResultaatRequestDto>().Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        //
        // 8. ZaakBesluit

        config
            .NewConfig<ZaakBesluit, ZaakBesluitResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id);

        //
        // 9. Audittrail

        config
            .NewConfig<AuditTrailRegel, AuditTrailRegelDto>()
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Wijzigingen, src => ConvertWijzigingenToDto(src.Oud, src.Nieuw))
            .Map(dest => dest.AanmaakDatum, src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true));

        //
        // 10. KlantContact

        config
            .NewConfig<KlantContact, KlantContactResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.DatumTijd, src => ProfileHelper.StringDateFromDateTime(src.DatumTijd, true));
    }

    // Note: The two factories below are Shape-B ConstructUsing configs (no ForAllMembers(Ignore) /
    // no blanket .MapWith) - the surrounding NewConfig's own .Map/.Ignore rules (Url/Uuid/Zaak/etc.)
    // still apply on top of whatever this factory returns. Both factory bodies recursively adapt a
    // nested source into a nested destination type; `config` is threaded through explicitly and
    // used via `.Adapt<T>(config)` so that the recursive call resolves against THIS local config
    // (which holds the custom rules registered above, e.g. OverigeZaakObject -> OverigeZaakObjectDto's
    // JToken.Parse conversion) rather than Mapster's ambient TypeAdapterConfig.GlobalSettings, which
    // would not have those rules and would silently produce different (wrong) results for those
    // members. `.Adapt<T>(config)` on a null source returns null (Mapster's own null-source handling),
    // matching AutoMapper's context.Mapper.Map<T>(null) behavior.

    private static ZaakRolResponseDto CreateZaakRolResponseDto(ZaakRol source, TypeAdapterConfig config)
    {
        // when corresponding relation i.e. source.NatuurlijkPersoon == null, we return base ZaakRolResponseDto,
        // because we don't need to include BetrokkeneIdentificatie in GetAll requests
        return source.BetrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => new NatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.NatuurlijkPersoon.Adapt<NatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.niet_natuurlijk_persoon => new NietNatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.NietNatuurlijkPersoon.Adapt<NietNatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.vestiging => new VestigingZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.Vestiging.Adapt<VestigingZaakRolDto>(config),
            },
            BetrokkeneType.organisatorische_eenheid => new OrganisatorischeEenheidZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.OrganisatorischeEenheid.Adapt<OrganisatorischeEenheidZaakRolDto>(config),
            },
            BetrokkeneType.medewerker => new MedewerkerZaakRolResponseDto
            {
                BetrokkeneIdentificatie = source.Medewerker.Adapt<MedewerkerZaakRolDto>(config),
            },
            _ => new ZaakRolResponseDto(),
        };
    }

    private static ZaakObjectResponseDto CreateZaakObjectResponseDto(ZaakObject source, TypeAdapterConfig config)
    {
        return source.ObjectType switch
        {
            ObjectType.adres => new AdresZaakObjectResponseDto { ObjectIdentificatie = source.Adres.Adapt<AdresZaakObjectDto>(config) },
            ObjectType.buurt => new BuurtZaakObjectResponseDto { ObjectIdentificatie = source.Buurt.Adapt<BuurtZaakObjectDto>(config) },
            ObjectType.pand => new PandZaakObjectResponseDto { ObjectIdentificatie = source.Pand.Adapt<PandZaakObjectDto>(config) },
            ObjectType.kadastrale_onroerende_zaak => new KadastraleOnroerendeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.KadastraleOnroerendeZaak.Adapt<KadastraleOnroerendeZaakObjectDto>(config),
            },
            ObjectType.gemeente => new GemeenteZaakObjectResponseDto { ObjectIdentificatie = source.Gemeente.Adapt<GemeenteZaakObjectDto>(config) },
            ObjectType.terrein_gebouwd_object => new TerreinGebouwdObjectZaakObjectResponseDto
            {
                ObjectIdentificatie = source.TerreinGebouwdObject.Adapt<TerreinGebouwdObjectZaakObjectDto>(config),
            },
            ObjectType.overige => new OverigeZaakObjectResponseDto { ObjectIdentificatie = source.Overige.Adapt<OverigeZaakObjectDto>(config) },
            ObjectType.woz_waarde => new WozWaardeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.WozWaardeObject.Adapt<WozWaardeZaakObjectDto>(config),
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

    private static WijzigingDto ConvertWijzigingenToDto(string oud, string nieuw)
    {
        var result = new WijzigingDto();

        var settings = new ZGWJsonSerializerSettings();

        if (!string.IsNullOrEmpty(oud))
        {
            result.Oud = JsonConvert.DeserializeObject(oud, settings);
        }
        if (!string.IsNullOrEmpty(nieuw))
        {
            result.Nieuw = JsonConvert.DeserializeObject(nieuw, settings);
        }
        return result;
    }
}
