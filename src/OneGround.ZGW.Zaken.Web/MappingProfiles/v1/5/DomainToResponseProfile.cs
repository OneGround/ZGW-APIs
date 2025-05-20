using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
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

// Note: This Profile adds extended mappings (above the ones defined in v1.0 and v1.2)
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
            .ForMember(dest => dest.Rollen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakRol>>(src => src.ZaakRollen))
            .ForMember(
                dest => dest.ZaakInformatieObjecten,
                opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakInformatieObject>>(src => src.ZaakInformatieObjecten)
            )
            .ForMember(dest => dest.ZaakObjecten, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakObject>>(src => src.ZaakObjecten))
            .ForMember(
                dest => dest.Status,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakStatussen != null);
                    opt.MapFrom<MemberUrlResolver, ZaakStatus>(src => src.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault());
                }
            )
            .ForMember(dest => dest.StartdatumBewaartermijn, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.StartdatumBewaartermijn)))
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.Toelichting)))
            .ForMember(
                dest => dest.BetalingsindicatieWeergave,
                opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.BetalingsindicatieWeergave))
            )
            .ForMember(
                dest => dest.OpdrachtgevendeOrganisatie,
                opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.OpdrachtgevendeOrganisatie))
            )
            .ForMember(dest => dest.Processobjectaard, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.Processobjectaard)));

        CreateMap<ZaakProcessobject, ZaakProcessobjectDto>();

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
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum)))
            .ForMember(dest => dest.OpdrachtgevendeOrganisatie, opt => opt.MapFrom(src => src.OpdrachtgevendeOrganisatie))
            .ForMember(dest => dest.Processobjectaard, opt => opt.MapFrom(src => src.Processobjectaard))
            .ForMember(
                dest => dest.StartdatumBewaartermijn,
                opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.StartdatumBewaartermijn))
            );

        CreateMap<ZaakProcessobject, ZaakProcessobjectDto>()
            .ForMember(dest => dest.Datumkenmerk, opt => opt.MapFrom(src => src.Datumkenmerk))
            .ForMember(dest => dest.Identificatie, opt => opt.MapFrom(src => src.Identificatie))
            .ForMember(dest => dest.Objecttype, opt => opt.MapFrom(src => src.Objecttype))
            .ForMember(dest => dest.Registratie, opt => opt.MapFrom(src => src.Registratie));

        //
        // 2. ZaakStatus

        CreateMap<ZaakStatus, ZaakStatusCreateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.DatumStatusGezet, opt => opt.MapFrom(src => ProfileHelper.SortableStringDateFromDate(src.DatumStatusGezet)));

        CreateMap<ZaakStatus, ZaakStatusGetResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.DatumStatusGezet, opt => opt.MapFrom(src => ProfileHelper.SortableStringDateFromDate(src.DatumStatusGezet)))
            .ForMember(
                dest => dest.ZaakInformatieObjecten,
                opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakInformatieObject>>(src => src.Zaak.ZaakInformatieObjecten)
            );

        //
        // 4. ZaakObjecten

        CreateMap<ZaakObject, ZaakObjectResponseDto>()
            .ConstructUsing(CreateZaakObjectResponseDto)
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.MapFrom(src => src.ObjectTypeOverigeDefinitie)) // Note: Supported in >= v1.2
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        // Note: This map is used to merge an existing ZAAKOBJECT with the PATCH operation
        CreateMap<ZaakObject, ZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.MapFrom(src => src.ObjectTypeOverigeDefinitie)) // Note: Supported in v1.2 only
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        CreateMap<AdresZaakObject, AdresZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<BuurtZaakObject, BuurtZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<GemeenteZaakObject, GemeenteZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<KadastraleOnroerendeZaakObject, KadastraleOnroerendeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<OverigeZaakObject, OverigeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<PandZaakObject, PandZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<TerreinGebouwdObjectZaakObject, TerreinGebouwdObjectZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.Ignore())
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.Ignore());

        CreateMap<WozWaardeZaakObject, WozWaardeZaakObjectRequestDto>()
            .ForMember(dest => dest.ObjectIdentificatie, opt => opt.MapFrom(src => src))
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
            .ForMember(dest => dest.RegistratieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.RegistratieDatum, true)))
            .ForMember(dest => dest.VernietigingsDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.VernietigingsDatum, true)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom<MemberUrlResolver, ZaakStatus>(src => src.Status));

        // Note: This map is used to merge an existing ZAAKINFORMATIEOBJECT with the PATCH operation
        CreateMap<ZaakInformatieObject, ZaakInformatieObjectRequestDto>()
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak))
            .ForMember(dest => dest.Status, opt => opt.MapFrom<MemberUrlResolver, ZaakStatus>(src => src.Status));

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
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Registratiedatum, true)))
            // Note: Map only zaak-statussen which matches zaak-status.GezetDoor
            .ForMember(
                dest => dest.Statussen,
                opt =>
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakStatus>>(src =>
                        src.Zaak.ZaakStatussen != null
                            ? src.Zaak.ZaakStatussen.Where(s => s.GezetDoor == src.Betrokkene).OrderBy(s => s.DatumStatusGezet)
                            : null
                    )
            );

        CreateMap<ContactpersoonRol, ContactpersoonRolDto>();

        // Note: We cannot use the v1.0 mapper because VestigingZaakRolDto contains a new field KvkNummer
        CreateMap<VestigingZaakRol, VestigingZaakRolDto>();

        //
        // 11. ZaakVerzoek

        CreateMap<ZaakVerzoek, ZaakVerzoekResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));

        //
        // 12. ZaakContactmoment

        CreateMap<ZaakContactmoment, ZaakContactmomentResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));
    }

    private static ZaakRolResponseDto CreateZaakRolResponseDto(ZaakRol source, ResolutionContext context)
    {
        // when corresponding relation i.e. source.NatuurlijkPersoon == null, we return base ZaakRolResponseDto,
        // because we don't need to include BetrokkeneIdentificatie in GetAll requests
        return source.BetrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => new NatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.NatuurlijkPersoonZaakRolDto>(source.NatuurlijkPersoon),
            },
            BetrokkeneType.niet_natuurlijk_persoon => new NietNatuurlijkPersoonZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.NietNatuurlijkPersoonZaakRolDto>(source.NietNatuurlijkPersoon),
            },
            BetrokkeneType.vestiging => new VestigingZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<VestigingZaakRolDto>(source.Vestiging), // Note: VestigingZaakRolDto contains one new field KvKNummer so it uses not the v1 Dto here
            },
            BetrokkeneType.organisatorische_eenheid => new OrganisatorischeEenheidZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.OrganisatorischeEenheidZaakRolDto>(source.OrganisatorischeEenheid),
            },
            BetrokkeneType.medewerker => new MedewerkerZaakRolResponseDto
            {
                BetrokkeneIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.MedewerkerZaakRolDto>(source.Medewerker),
            },
            _ => new ZaakRolResponseDto(),
        };
    }

    private static ZaakObjectResponseDto CreateZaakObjectResponseDto(ZaakObject source, ResolutionContext context)
    {
        return source.ObjectType switch
        {
            ObjectType.adres => new AdresZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.AdresZaakObjectDto>(source.Adres),
            },
            ObjectType.buurt => new BuurtZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.BuurtZaakObjectDto>(source.Buurt),
            },
            ObjectType.pand => new PandZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.PandZaakObjectDto>(source.Pand),
            },
            ObjectType.kadastrale_onroerende_zaak => new KadastraleOnroerendeZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto>(source.KadastraleOnroerendeZaak),
            },
            ObjectType.gemeente => new GemeenteZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.GemeenteZaakObjectDto>(source.Gemeente),
            },
            ObjectType.terrein_gebouwd_object => new TerreinGebouwdObjectZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto>(source.TerreinGebouwdObject),
            },
            ObjectType.overige => new OverigeZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.OverigeZaakObjectDto>(source.Overige),
            },
            ObjectType.woz_waarde => new WozWaardeZaakObjectResponseDto
            {
                ObjectIdentificatie = context.Mapper.Map<Zaken.Contracts.v1.WozWaardeZaakObjectDto>(source.WozWaardeObject),
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
