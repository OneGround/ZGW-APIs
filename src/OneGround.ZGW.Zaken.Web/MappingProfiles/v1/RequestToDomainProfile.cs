using System;
using System.Linq;
using AutoMapper;
using NodaTime.Text;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        //
        // 1. Map Get all Zaken via query-parameters GetAllZakenQueryParameters to internal GetAllZakenFilter model

        CreateMap<GetAllZakenQueryParameters, GetAllZakenFilter>()
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum)))
            .ForMember(dest => dest.Archiefactiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt)))
            .ForMember(dest => dest.Archiefactiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt)))
            .ForMember(
                dest => dest.Archiefnominatie__in,
                opt => opt.MapFrom(src => ProfileHelper.EnumArrayFromString<ArchiefNominatie>(src.Archiefnominatie__in))
            )
            .ForMember(
                dest => dest.Archiefstatus__in,
                opt => opt.MapFrom(src => ProfileHelper.EnumArrayFromString<ArchiefStatus>(src.Archiefstatus__in))
            )
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum)))
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte)));

        //
        // 2. Map POST Zaak (geometry) search ZaakSearchRequestDto to internal GetAllZakenFilter model

        CreateMap<ZaakSearchRequestDto, GetAllZakenFilter>()
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum)))
            .ForMember(dest => dest.Archiefactiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt)))
            .ForMember(dest => dest.Archiefactiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt)))
            .ForMember(
                dest => dest.Archiefnominatie__in,
                opt => opt.MapFrom(src => ProfileHelper.EnumArrayFromString<ArchiefNominatie>(src.Archiefnominatie__in))
            )
            .ForMember(
                dest => dest.Archiefstatus__in,
                opt => opt.MapFrom(src => ProfileHelper.EnumArrayFromString<ArchiefStatus>(src.Archiefstatus__in))
            )
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum)))
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte)));

        CreateMap<ZaakRequestDto, Zaak>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Einddatum, opt => opt.Ignore())
            .ForMember(dest => dest.BetalingsindicatieWeergave, opt => opt.Ignore())
            .ForMember(dest => dest.Deelzaken, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakEigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakStatussen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjecten, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakInformatieObjecten, opt => opt.Ignore())
            .ForMember(dest => dest.Resultaat, opt => opt.Ignore())
            .ForMember(dest => dest.HoofdzaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Hoofdzaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRollen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakBesluiten, opt => opt.Ignore())
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum)))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.Startdatum)))
            .ForMember(dest => dest.Omschrijving, opt => opt.MapFrom(src => src.Omschrijving))
            .ForMember(dest => dest.EinddatumGepland, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland)))
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            )
            .ForMember(dest => dest.Publicatiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Publicatiedatum)))
            .ForMember(dest => dest.LaatsteBetaaldatum, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.LaatsteBetaaldatum)))
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum)))
            .ForMember(dest => dest.KlantContacten, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.OpdrachtgevendeOrganisatie, opt => opt.Ignore())
            .ForMember(dest => dest.Processobjectaard, opt => opt.Ignore())
            .ForMember(dest => dest.StartdatumBewaartermijn, opt => opt.Ignore())
            .ForMember(dest => dest.Processobject, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaaktype, opt => opt.MapFrom(src => src.Zaaktype.TrimEnd('/')));

        CreateMap<RelevanteAndereZaakDto, RelevanteAndereZaak>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<ZaakKenmerkDto, ZaakKenmerk>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<ZaakVerlengingDto, ZaakVerlenging>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Duur, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Duur).Value));

        CreateMap<ZaakOpschortingDto, ZaakOpschorting>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore());

        //
        // 2. ZaakStatus

        CreateMap<GetAllZaakStatussenQueryParameters, GetAllZaakStatussenFilter>();

        CreateMap<ZaakStatusRequestDto, ZaakStatus>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.DatumStatusGezet, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.DatumStatusGezet)))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieLaatstGezetteStatus, opt => opt.Ignore())
            .ForMember(dest => dest.GezetDoor, opt => opt.Ignore());

        //
        // 3. ZaakObjecten

        CreateMap<GetAllZaakObjectenQueryParameters, GetAllZaakObjectenFilter>();

        CreateMap<Zaken.Contracts.v1._2.ObjectTypeOverigeDefinitieDto, ObjectTypeOverigeDefinitie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore());

        CreateMap<AdresZaakObjectRequestDto, AdresZaakObject>().ConstructUsing(CreateAdresZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<BuurtZaakObjectRequestDto, BuurtZaakObject>().ConstructUsing(CreateBuurtZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<PandZaakObjectRequestDto, PandZaakObject>().ConstructUsing(CreatePandZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<KadastraleOnroerendeZaakObjectRequestDto, KadastraleOnroerendeZaakObject>()
            .ConstructUsing(CreateKadastraleOnroerendeZaakObject)
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<GemeenteZaakObjectRequestDto, GemeenteZaakObject>().ConstructUsing(CreateGemeenteZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<TerreinGebouwdObjectZaakObjectRequestDto, TerreinGebouwdObjectZaakObject>()
            .ConstructUsing(CreateTerreinGebouwdObjectZaakObject)
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<OverigeZaakObjectRequestDto, OverigeZaakObject>().ConstructUsing(CreateOverigeZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<WozWaardeZaakObjectRequestDto, WozWaardeZaakObject>().ConstructUsing(CreateWozWaardeZaakObject).ForAllMembers(opt => opt.Ignore());

        CreateMap<ZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.MapFrom(src => src.Object))
            .ForMember(dest => dest.ZaakObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectType, opt => opt.MapFrom(src => src.ObjectType))
            .ForMember(dest => dest.ObjectTypeOverige, opt => opt.MapFrom(src => src.ObjectTypeOverige))
            .ForMember(dest => dest.ObjectTypeOverigeDefinitie, opt => opt.MapFrom(src => src.ObjectTypeOverigeDefinitie)) // Note: Supported in v1.2 only
            .ForMember(dest => dest.RelatieOmschrijving, opt => opt.MapFrom(src => src.RelatieOmschrijving))
            .ForMember(dest => dest.Adres, opt => opt.Ignore())
            .ForMember(dest => dest.Buurt, opt => opt.Ignore())
            .ForMember(dest => dest.Pand, opt => opt.Ignore())
            .ForMember(dest => dest.KadastraleOnroerendeZaak, opt => opt.Ignore())
            .ForMember(dest => dest.Gemeente, opt => opt.Ignore())
            .ForMember(dest => dest.TerreinGebouwdObject, opt => opt.Ignore())
            .ForMember(dest => dest.Overige, opt => opt.Ignore())
            .ForMember(dest => dest.WozWaardeObject, opt => opt.Ignore())
            .IncludeAllDerived();

        CreateMap<AdresZaakObjectDto, AdresZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<AdresZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Adres, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<BuurtZaakObjectDto, BuurtZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<BuurtZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Buurt, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<PandZaakObjectDto, PandZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<PandZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Pand, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<KadastraleOnroerendeZaakObjectDto, KadastraleOnroerendeZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<KadastraleOnroerendeZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.KadastraleOnroerendeZaak, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<GemeenteZaakObjectDto, GemeenteZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<GemeenteZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Gemeente, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<TerreinGebouwdObjectZaakObjectDto, TerreinGebouwdObjectZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.AdresAanduidingGrp_AoaHuisletter, opt => opt.MapFrom(src => src.AdresAanduidingGrp.AoaHuisletter))
            .ForMember(dest => dest.AdresAanduidingGrp_AoaHuisnummer, opt => opt.MapFrom(src => src.AdresAanduidingGrp.AoaHuisnummer))
            .ForMember(
                dest => dest.AdresAanduidingGrp_AoaHuisnummertoevoeging,
                opt => opt.MapFrom(src => src.AdresAanduidingGrp.AoaHuisnummertoevoeging)
            )
            .ForMember(dest => dest.AdresAanduidingGrp_AoaPostcode, opt => opt.MapFrom(src => src.AdresAanduidingGrp.AoaPostcode))
            .ForMember(dest => dest.AdresAanduidingGrp_GorOpenbareRuimteNaam, opt => opt.MapFrom(src => src.AdresAanduidingGrp.GorOpenbareRuimteNaam))
            .ForMember(dest => dest.AdresAanduidingGrp_NumIdentificatie, opt => opt.MapFrom(src => src.AdresAanduidingGrp.NumIdentificatie))
            .ForMember(dest => dest.AdresAanduidingGrp_OaoIdentificatie, opt => opt.MapFrom(src => src.AdresAanduidingGrp.OaoIdentificatie))
            .ForMember(dest => dest.AdresAanduidingGrp_OgoLocatieAanduiding, opt => opt.MapFrom(src => src.AdresAanduidingGrp.OgoLocatieAanduiding))
            .ForMember(dest => dest.AdresAanduidingGrp_WplWoonplaatsNaam, opt => opt.MapFrom(src => src.AdresAanduidingGrp.WplWoonplaatsNaam));
        CreateMap<TerreinGebouwdObjectZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.TerreinGebouwdObject, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<OverigeZaakObjectDto, OverigeZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<OverigeZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Overige, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<AanduidingWozObjectDto, AanduidingWozObject>().ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<WozObjectDto, WozObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AanduidingWozObjectId, opt => opt.Ignore());
        CreateMap<WozWaardeZaakObjectDto, WozWaardeZaakObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObject, opt => opt.Ignore())
            .ForMember(dest => dest.IsVoorId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<WozWaardeZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.WozWaardeObject, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        //
        // 4. ZaakInformatieObjecten

        CreateMap<GetAllZaakInformatieObjectenQueryParameters, GetAllZaakInformatieObjectenFilter>();

        CreateMap<ZaakInformatieObjectRequestDto, ZaakInformatieObject>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RegistratieDatum, opt => opt.Ignore())
            .ForMember(dest => dest.AardRelatieWeergave, opt => opt.Ignore())
            .ForMember(dest => dest.VernietigingsDatum, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        //
        // 5. ZaakRol

        CreateMap<GetAllZaakRollenQueryParameters, GetAllZaakRollenFilter>()
            .ForMember(dest => dest.NatuurlijkPersoonInpBsn, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn))
            .ForMember(
                dest => dest.NatuurlijkPersoonInpANummer,
                opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer)
            )
            .ForMember(
                dest => dest.NatuurlijkPersoonAnpIdentificatie,
                opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie)
            )
            .ForMember(
                dest => dest.NietNatuurlijkPersoonAnnIdentificatie,
                opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie)
            )
            .ForMember(
                dest => dest.NietNatuurlijkPersoonInnNnpId,
                opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId)
            )
            .ForMember(dest => dest.VestigingNummer, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__vestiging__vestigingsNummer))
            .ForMember(
                dest => dest.OrganisatorischeEenheidIdentificatie,
                opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__organisatorischeEenheid__identificatie)
            )
            .ForMember(dest => dest.MedewerkerIdentificatie, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie__medewerker__identificatie));

        CreateMap<ZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Betrokkene, opt => opt.MapFrom(src => src.Betrokkene))
            .ForMember(dest => dest.BetrokkeneType, opt => opt.MapFrom(src => src.BetrokkeneType))
            .ForMember(dest => dest.AfwijkendeNaamBetrokkene, opt => opt.Ignore())
            .ForMember(dest => dest.RolType, opt => opt.MapFrom(src => src.RolType))
            .ForMember(dest => dest.Roltoelichting, opt => opt.MapFrom(src => src.RolToelichting))
            .ForMember(dest => dest.Registratiedatum, opt => opt.Ignore())
            .ForMember(dest => dest.Omschrijving, opt => opt.Ignore())
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieMachtiging, opt => opt.MapFrom(src => src.IndicatieMachtiging))
            .ForMember(dest => dest.ContactpersoonRolId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactpersoonRol, opt => opt.Ignore())
            .ForMember(dest => dest.NatuurlijkPersoon, opt => opt.Ignore())
            .ForMember(dest => dest.NietNatuurlijkPersoon, opt => opt.Ignore())
            .ForMember(dest => dest.Vestiging, opt => opt.Ignore())
            .ForMember(dest => dest.Medewerker, opt => opt.Ignore())
            .ForMember(dest => dest.OrganisatorischeEenheid, opt => opt.Ignore())
            .IncludeAllDerived();

        CreateMap<VerblijfsadresDto, Verblijfsadres>().ForMember(dest => dest.Id, opt => opt.Ignore());
        CreateMap<SubVerblijfBuitenlandDto, SubVerblijfBuitenland>().ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<NatuurlijkPersoonZaakRolDto, NatuurlijkPersoonZaakRol>()
            .ForMember(dest => dest.VerblijfsadresId, opt => opt.Ignore())
            .ForMember(dest => dest.SubVerblijfBuitenlandId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRol, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRolId, opt => opt.Ignore());
        CreateMap<NatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.NatuurlijkPersoon, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        CreateMap<NietNatuurlijkPersoonZaakRolDto, NietNatuurlijkPersoonZaakRol>()
            .ForMember(dest => dest.SubVerblijfBuitenlandId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRol, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRolId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
        CreateMap<NietNatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.NietNatuurlijkPersoon, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<VestigingZaakRolDto, VestigingZaakRol>()
            .ForMember(dest => dest.VerblijfsadresId, opt => opt.Ignore())
            .ForMember(dest => dest.SubVerblijfBuitenlandId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRol, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRolId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.KvkNummer, opt => opt.Ignore());

        CreateMap<VestigingZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.Vestiging, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<OrganisatorischeEenheidZaakRolDto, OrganisatorischeEenheidZaakRol>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRol, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRolId, opt => opt.Ignore());
        CreateMap<OrganisatorischeEenheidZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.OrganisatorischeEenheid, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        CreateMap<MedewerkerZaakRolDto, MedewerkerZaakRol>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRol, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakRolId, opt => opt.Ignore());
        CreateMap<MedewerkerZaakRolRequestDto, ZaakRol>().ForMember(dest => dest.Medewerker, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        //
        // 6. ZaakResultaat

        CreateMap<ZaakResultaatRequestDto, ZaakResultaat>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ResultaatType, opt => opt.MapFrom(src => src.ResultaatType))
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => src.Toelichting));

        CreateMap<GetAllZaakResultatenQueryParameters, GetAllZaakResultatenFilter>();

        //
        // 7. ZaakEigenschap

        CreateMap<ZaakEigenschapRequestDto, ZaakEigenschap>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Eigenschap, opt => opt.MapFrom(src => src.Eigenschap))
            .ForMember(dest => dest.Naam, opt => opt.Ignore())
            .ForMember(dest => dest.Waarde, opt => opt.MapFrom(src => src.Waarde))
            .ForMember(dest => dest.ZaakId, opt => opt.MapFrom(src => ExtractIdFromZaak(src.Zaak)));

        //
        // 8. ZaakBesluit

        CreateMap<ZaakBesluitRequestDto, ZaakBesluit>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Besluit, opt => opt.MapFrom(src => src.Besluit));

        //
        // 9. KlantContact

        CreateMap<GetAllKlantContactenQueryParameters, GetAllKlantContactenFilter>();

        CreateMap<KlantContactRequestDto, KlantContact>()
            .ForMember(dest => dest.DatumTijd, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.DatumTijd)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore());
    }

    private static Guid ExtractIdFromZaak(string zaakUrl)
    {
        if (!Guid.TryParse(zaakUrl.TrimEnd('/').Split('/').Last(), out var id))
        {
            throw new InvalidOperationException($"Could not parse id from zaak-resource {zaakUrl}.");
        }
        return id;
    }

    private static GemeenteZaakObject CreateGemeenteZaakObject(GemeenteZaakObjectRequestDto source, ResolutionContext context)
    {
        return new GemeenteZaakObject
        {
            GemeenteCode = source.ObjectIdentificatie.GemeenteCode,
            GemeenteNaam = source.ObjectIdentificatie.GemeenteNaam,
        };
    }

    private static TerreinGebouwdObjectZaakObject CreateTerreinGebouwdObjectZaakObject(
        TerreinGebouwdObjectZaakObjectRequestDto source,
        ResolutionContext context
    )
    {
        var terreinGebouwdObjectZaakObject = new TerreinGebouwdObjectZaakObject { Identificatie = source.ObjectIdentificatie.Identificatie };

        if (source.ObjectIdentificatie.AdresAanduidingGrp != null)
        {
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_NumIdentificatie = source.ObjectIdentificatie.AdresAanduidingGrp.NumIdentificatie;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_OaoIdentificatie = source.ObjectIdentificatie.AdresAanduidingGrp.OaoIdentificatie;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_WplWoonplaatsNaam = source.ObjectIdentificatie.AdresAanduidingGrp.WplWoonplaatsNaam;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_GorOpenbareRuimteNaam = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .GorOpenbareRuimteNaam;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaPostcode = source.ObjectIdentificatie.AdresAanduidingGrp.AoaPostcode;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisnummer = source.ObjectIdentificatie.AdresAanduidingGrp.AoaHuisnummer;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisletter = source.ObjectIdentificatie.AdresAanduidingGrp.AoaHuisletter;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisnummertoevoeging = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .AoaHuisnummertoevoeging;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_OgoLocatieAanduiding = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .OgoLocatieAanduiding;
        }

        return terreinGebouwdObjectZaakObject;
    }

    private static OverigeZaakObject CreateOverigeZaakObject(OverigeZaakObjectRequestDto source, ResolutionContext context)
    {
        return new OverigeZaakObject { OverigeData = source.ObjectIdentificatie.OverigeData };
    }

    private static AdresZaakObject CreateAdresZaakObject(AdresZaakObjectRequestDto source, ResolutionContext context)
    {
        return new AdresZaakObject
        {
            Huisletter = source.ObjectIdentificatie.Huisletter,
            Huisnummer = source.ObjectIdentificatie.Huisnummer,
            HuisnummerToevoeging = source.ObjectIdentificatie.HuisnummerToevoeging,
            GorOpenbareRuimteNaam = source.ObjectIdentificatie.GorOpenbareRuimteNaam,
            Identificatie = source.ObjectIdentificatie.Identificatie,
            WplWoonplaatsNaam = source.ObjectIdentificatie.WplWoonplaatsNaam,
            Postcode = source.ObjectIdentificatie.Postcode,
        };
    }

    private static BuurtZaakObject CreateBuurtZaakObject(BuurtZaakObjectRequestDto source, ResolutionContext context)
    {
        return new BuurtZaakObject
        {
            BuurtCode = source.ObjectIdentificatie.BuurtCode,
            BuurtNaam = source.ObjectIdentificatie.BuurtNaam,
            GemGemeenteCode = source.ObjectIdentificatie.GemGemeenteCode,
            WykWijkCode = source.ObjectIdentificatie.WykWijkCode,
        };
    }

    private static PandZaakObject CreatePandZaakObject(PandZaakObjectRequestDto source, ResolutionContext context)
    {
        return new PandZaakObject { Identificatie = source.ObjectIdentificatie.Identificatie };
    }

    private static KadastraleOnroerendeZaakObject CreateKadastraleOnroerendeZaakObject(
        KadastraleOnroerendeZaakObjectRequestDto source,
        ResolutionContext context
    )
    {
        return new KadastraleOnroerendeZaakObject
        {
            KadastraleIdentificatie = source.ObjectIdentificatie.KadastraleIdentificatie,
            KadastraleAanduiding = source.ObjectIdentificatie.KadastraleAanduiding,
        };
    }

    private static WozWaardeZaakObject CreateWozWaardeZaakObject(WozWaardeZaakObjectRequestDto source, ResolutionContext context)
    {
        var result = new WozWaardeZaakObject { WaardePeildatum = source.ObjectIdentificatie.WaardePeildatum };

        if (source.ObjectIdentificatie.IsVoor != null)
        {
            result.IsVoor = new WozObject { WozObjectNummer = source.ObjectIdentificatie.IsVoor.WozObjectNummer };

            if (source.ObjectIdentificatie.IsVoor.AanduidingWozObject != null)
            {
                result.IsVoor.AanduidingWozObject = new AanduidingWozObject
                {
                    AoaIdentificatie = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaIdentificatie,
                    WplWoonplaatsNaam = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.WplWoonplaatsNaam,
                    GorOpenbareRuimteNaam = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.GorOpenbareRuimteNaam,
                    AoaPostcode = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaPostcode,
                    AoaHuisnummer = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisnummer,
                    AoaHuisletter = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisletter,
                    AoaHuisnummerToevoeging = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisnummerToevoeging,
                    LocatieOmschrijving = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.LocatieOmschrijving,
                };
            }
        }
        return result;
    }
}
