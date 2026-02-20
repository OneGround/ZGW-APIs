using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
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

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        //
        // 1. Zaak

        CreateMap<Zaak, ZaakResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Deelzaken, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<Zaak>>(src => src.Deelzaken))
            .ForMember(dest => dest.Hoofdzaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Hoofdzaak))
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Registratiedatum)))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Startdatum)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Einddatum)))
            .ForMember(dest => dest.EinddatumGepland, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EinddatumGepland)))
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening,
                opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.UiterlijkeEinddatumAfdoening))
            )
            .ForMember(dest => dest.Publicatiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Publicatiedatum)))
            .ForMember(dest => dest.LaatsteBetaaldatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.LaatsteBetaaldatum, true)))
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum)))
            .ForMember(dest => dest.Eigenschappen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakEigenschap>>(src => src.ZaakEigenschappen))
            .ForMember(dest => dest.Resultaat, opt => opt.MapFrom<MemberUrlResolver, ZaakResultaat>(src => src.Resultaat))
            .ForMember(
                dest => dest.Status,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakStatussen != null);
                    opt.MapFrom<MemberUrlResolver, ZaakStatus>(src => src.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault());
                }
            )
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.Toelichting)))
            .ForMember(
                dest => dest.BetalingsindicatieWeergave,
                opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.BetalingsindicatieWeergave))
            );

        CreateMap<RelevanteAndereZaak, RelevanteAndereZaakDto>();
        CreateMap<ZaakKenmerk, ZaakKenmerkDto>();
        CreateMap<ZaakVerlenging, ZaakVerlengingDto>();
        CreateMap<ZaakOpschorting, ZaakOpschortingDto>();

        // Note: This map is used to merge an existing ZAAK with the PATCH operation
        CreateMap<Zaak, ZaakRequestDto>()
            .ForMember(dest => dest.Hoofdzaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Hoofdzaak))
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Registratiedatum)))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Startdatum)))
            .ForMember(dest => dest.EinddatumGepland, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EinddatumGepland)))
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening,
                opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.UiterlijkeEinddatumAfdoening))
            )
            .ForMember(dest => dest.Publicatiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Publicatiedatum)))
            .ForMember(dest => dest.LaatsteBetaaldatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.LaatsteBetaaldatum, true)))
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum)));

        //
        // 2. ZaakStatus

        CreateMap<ZaakStatus, ZaakStatusResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.DatumStatusGezet, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.DatumStatusGezet, true)));

        //
        // 3. ZaakEigenschap

        CreateMap<ZaakEigenschap, ZaakEigenschapResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        //
        // 4. ZaakObjecten

        CreateMap<ObjectTypeOverigeDefinitie, ObjectTypeOverigeDefinitieDto>() // Note: Supported in v1.2 only
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url))
            .ForMember(dest => dest.Schema, opt => opt.MapFrom(src => src.Schema))
            .ForMember(dest => dest.ObjectData, opt => opt.MapFrom(src => src.ObjectData));

        CreateMap<ZaakObject, ZaakObjectResponseDto>()
            .ConstructUsing(CreateZaakObjectResponseDto)
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.MapFrom(src => src.ObjectTypeOverigeDefinitie)) // Note: Supported in v1.2 only
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.Version, opt => opt.Ignore());

        CreateMap<AdresZaakObject, AdresZaakObjectDto>();
        CreateMap<BuurtZaakObject, BuurtZaakObjectDto>();
        CreateMap<PandZaakObject, PandZaakObjectDto>();
        CreateMap<GemeenteZaakObject, GemeenteZaakObjectDto>();
        CreateMap<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectDto>();
        CreateMap<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectDto>()
            .ConstructUsing(s => new TerreinGebouwdObjectZaakObjectDto
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
            })
            .ForMember(dest => dest.Identificatie, opt => opt.MapFrom(src => src.Identificatie))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<OverigeZaakObject, OverigeZaakObjectDto>()
            .ForMember(dest => dest.OverigeData, opt => opt.MapFrom(src => JToken.Parse(src)));

        CreateMap<AanduidingWozObject, AanduidingWozObjectDto>();
        CreateMap<WozObject, WozObjectDto>();
        CreateMap<WozWaardeZaakObject, WozWaardeZaakObjectDto>();

        // Note: This maps is used to merge an existing ObjectTypeOverigeDefinitie with the PATCH operation
        CreateMap<ObjectTypeOverigeDefinitieDto, ObjectTypeOverigeDefinitie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore());

        CreateMap<ZaakObject, ZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.MapFrom(src => src.ObjectTypeOverigeDefinitie)) // Note: Supported in v1.2 only
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.Version, opt => opt.Ignore());

        CreateMap<AdresZaakObject, AdresZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<BuurtZaakObject, BuurtZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<GemeenteZaakObject, GemeenteZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<OverigeZaakObject, OverigeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<PandZaakObject, PandZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<WozWaardeZaakObject, WozWaardeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        //
        // 5. ZaakInformatieObjecten

        CreateMap<ZaakInformatieObject, ZaakInformatieObjectResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.AardRelatieWeergave, opt => opt.MapFrom(src => AardRelatieWeergaveToString(src.AardRelatieWeergave)))
            .ForMember(dest => dest.RegistratieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.RegistratieDatum, true)));

        // Note: This map is used to merge an existing ZAAKINFORMATIEOBJECT with the PATCH operation
        CreateMap<ZaakInformatieObject, ZaakInformatieObjectRequestDto>()
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        //
        // 6. ZaakRol

        CreateMap<ZaakRol, ZaakRolResponseDto>()
            .ConstructUsing(CreateZaakRolResponseDto)
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(
                dest => dest.IndicatieMachtiging,
                opt => opt.MapFrom(src => !src.IndicatieMachtiging.HasValue ? "" : src.IndicatieMachtiging.ToString())
            )
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Registratiedatum, true)));

        CreateMap<NatuurlijkPersoonZaakRol, NatuurlijkPersoonZaakRolDto>()
            .ForMember(dest => dest.Geboortedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Geboortedatum, true)));
        CreateMap<NietNatuurlijkPersoonZaakRol, NietNatuurlijkPersoonZaakRolDto>();
        CreateMap<VestigingZaakRol, VestigingZaakRolDto>();
        CreateMap<OrganisatorischeEenheidZaakRol, OrganisatorischeEenheidZaakRolDto>();
        CreateMap<MedewerkerZaakRol, MedewerkerZaakRolDto>();
        CreateMap<Verblijfsadres, VerblijfsadresDto>();
        CreateMap<SubVerblijfBuitenland, SubVerblijfBuitenlandDto>();

        //
        // 7. ZaakResultaat

        CreateMap<ZaakResultaat, ZaakResultaatResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id));

        // Note: This map is used to merge an existing ZaakResultaat with the PATCH operation
        CreateMap<ZaakResultaat, ZaakResultaatRequestDto>()
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        //
        // 8. ZaakBesluit

        CreateMap<ZaakBesluit, ZaakBesluitResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id));

        //
        // 9. Audittrail

        CreateMap<AuditTrailRegel, AuditTrailRegelDto>()
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Wijzigingen, opt => opt.MapFrom(src => ConvertWijzigingenToDto(src.Oud, src.Nieuw)))
            .ForMember(dest => dest.AanmaakDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true)));

        //
        // 10. KlantContact

        CreateMap<KlantContact, KlantContactResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.DatumTijd, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.DatumTijd, true)));
    }

    private static ZaakRolResponseDto CreateZaakRolResponseDto(ZaakRol source, ResolutionContext context)
    {
        // when corresponding relation i.e. source.NatuurlijkPersoon == null, we return base ZaakRolResponseDto,
        // because we don't need to include BetrokkeneIdentificatie in GetAll requests
        return source.BetrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => new NatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<NatuurlijkPersoonZaakRolDto>(source.NatuurlijkPersoon),
            },
            BetrokkeneType.niet_natuurlijk_persoon => new NietNatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<NietNatuurlijkPersoonZaakRolDto>(source.NietNatuurlijkPersoon),
            },
            BetrokkeneType.vestiging => new VestigingZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<VestigingZaakRolDto>(source.Vestiging),
            },
            BetrokkeneType.organisatorische_eenheid => new OrganisatorischeEenheidZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<OrganisatorischeEenheidZaakRolDto>(source.OrganisatorischeEenheid),
            },
            BetrokkeneType.medewerker => new MedewerkerZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<MedewerkerZaakRolDto>(source.Medewerker),
            },
            _ => new ZaakRolResponseDto(),
        };
    }

    private static ZaakObjectResponseDto CreateZaakObjectResponseDto(ZaakObject source, ResolutionContext context)
    {
        return source.ObjectType switch
        {
            ObjectType.adres => new AdresZaakObjectResponseDto { ObjectIdentificatie = context.Mapper.Map<AdresZaakObjectDto>(source.Adres) },
            ObjectType.buurt => new BuurtZaakObjectResponseDto { ObjectIdentificatie = context.Mapper.Map<BuurtZaakObjectDto>(source.Buurt) },
            ObjectType.pand => new PandZaakObjectResponseDto { ObjectIdentificatie = context.Mapper.Map<PandZaakObjectDto>(source.Pand) },
            ObjectType.kadastrale_onroerende_zaak => new KadastraleOnroerendeZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<KadastraleOnroerendeZaakObjectDto>(source.KadastraleOnroerendeZaak),
            },
            ObjectType.gemeente => new GemeenteZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<GemeenteZaakObjectDto>(source.Gemeente),
            },
            ObjectType.terrein_gebouwd_object => new TerreinGebouwdObjectZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(source.TerreinGebouwdObject),
            },
            ObjectType.overige => new OverigeZaakObjectResponseDto { ObjectIdentificatie = context.Mapper.Map<OverigeZaakObjectDto>(source.Overige) },
            ObjectType.woz_waarde => new WozWaardeZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<WozWaardeZaakObjectDto>(source.WozWaardeObject),
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
