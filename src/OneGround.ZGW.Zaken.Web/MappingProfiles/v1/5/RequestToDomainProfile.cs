using System;
using AutoMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._5;

// Note: This Profile adds extended mappings (above the ones defined in v1.0)
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
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte)))
            .ForMember(dest => dest.Bronorganisatie__in, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.Bronorganisatie__in)))
            .ForMember(dest => dest.Uuid__in, opt => opt.MapFrom(src => Array.Empty<Guid>()))
            .ForMember(dest => dest.Zaaktype__in, opt => opt.MapFrom(src => Array.Empty<string>()))
            .ForMember(
                dest => dest.Archiefactiedatum__isnull,
                opt => opt.MapFrom(src => ProfileHelper.BooleanFromString(src.Archiefactiedatum__isnull))
            )
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum)))
            .ForMember(dest => dest.Registratiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__gt)))
            .ForMember(dest => dest.Registratiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__lt)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum)))
            .ForMember(dest => dest.Einddatum__isnull, opt => opt.MapFrom(src => ProfileHelper.BooleanFromString(src.Einddatum__isnull)))
            .ForMember(dest => dest.Einddatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum__gt)))
            .ForMember(dest => dest.Einddatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum__lt)))
            .ForMember(dest => dest.EinddatumGepland, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland)))
            .ForMember(dest => dest.EinddatumGepland__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__gt)))
            .ForMember(dest => dest.EinddatumGepland__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__lt)))
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            )
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening__gt,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__gt))
            )
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening__lt,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__lt))
            )
            .ForMember(dest => dest.Rol__betrokkeneType, opt => opt.MapFrom(src => src.Rol__betrokkeneType))
            .ForMember(dest => dest.Rol__betrokkene, opt => opt.MapFrom(src => src.Rol__betrokkene))
            .ForMember(dest => dest.Rol__omschrijvingGeneriek, opt => opt.MapFrom(src => src.Rol__omschrijvingGeneriek))
            .ForMember(dest => dest.MaximaleVertrouwelijkheidaanduiding, opt => opt.MapFrom(src => src.MaximaleVertrouwelijkheidaanduiding))
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__medewerker__identificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__medewerker__identificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie)
            );

        //
        // 2. Map POST Zaak (geometry) search ZaakSearchRequestDto to internal GetAllZakenFilter model

        CreateMap<ZaakSearchRequestDto, GetAllZakenFilter>()
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum)))
            .ForMember(dest => dest.Archiefactiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt)))
            .ForMember(dest => dest.Archiefactiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt)))
            .ForMember(dest => dest.Archiefnominatie__in, opt => opt.MapFrom(src => src.Archiefnominatie__in))
            .AfterMap(
                (_, dest) =>
                {
                    dest.Archiefnominatie__in ??= Array.Empty<ArchiefNominatie>();
                }
            )
            .ForMember(dest => dest.Archiefstatus__in, opt => opt.MapFrom(src => src.Archiefstatus__in))
            .AfterMap(
                (_, dest) =>
                {
                    dest.Archiefstatus__in ??= Array.Empty<ArchiefStatus>();
                }
            )
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum)))
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte)))
            .ForMember(dest => dest.Bronorganisatie__in, opt => opt.MapFrom(src => src.Bronorganisatie__in))
            .AfterMap(
                (_, dest) =>
                {
                    dest.Bronorganisatie__in ??= Array.Empty<string>();
                }
            )
            .ForMember(dest => dest.Uuid__in, opt => opt.MapFrom(src => src.Uuid__in))
            .AfterMap(
                (_, dest) =>
                {
                    dest.Uuid__in ??= Array.Empty<Guid>();
                }
            )
            .ForMember(dest => dest.Zaaktype__in, opt => opt.MapFrom(src => src.Zaaktype__in))
            .AfterMap(
                (_, dest) =>
                {
                    dest.Zaaktype__in ??= Array.Empty<string>();
                }
            )
// Removed duplicate mapping for Archiefnominatie__in
            .ForMember(dest => dest.Archiefactiedatum__isnull, opt => opt.MapFrom(src => src.Archiefactiedatum__isnull))
            .ForMember(dest => dest.Registratiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum)))
            .ForMember(dest => dest.Registratiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__gt)))
            .ForMember(dest => dest.Registratiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__lt)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum)))
            .ForMember(dest => dest.Einddatum__isnull, opt => opt.MapFrom(src => ProfileHelper.BooleanFromString(src.Einddatum__isnull)))
            .ForMember(dest => dest.Einddatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum__gt)))
            .ForMember(dest => dest.Einddatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Einddatum__lt)))
            .ForMember(dest => dest.EinddatumGepland, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland)))
            .ForMember(dest => dest.EinddatumGepland__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__gt)))
            .ForMember(dest => dest.EinddatumGepland__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__lt)))
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            )
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening__gt,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__gt))
            )
            .ForMember(
                dest => dest.UiterlijkeEinddatumAfdoening__lt,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__lt))
            )
            .ForMember(dest => dest.Rol__betrokkeneType, opt => opt.MapFrom(src => src.Rol__betrokkeneType))
            .ForMember(dest => dest.Rol__betrokkene, opt => opt.MapFrom(src => src.Rol__betrokkene))
            .ForMember(dest => dest.Rol__omschrijvingGeneriek, opt => opt.MapFrom(src => src.Rol__omschrijvingGeneriek))
            .ForMember(dest => dest.MaximaleVertrouwelijkheidaanduiding, opt => opt.MapFrom(src => src.MaximaleVertrouwelijkheidaanduiding))
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__medewerker__identificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__medewerker__identificatie)
            )
            .ForMember(
                dest => dest.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie,
                opt => opt.MapFrom(src => src.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie)
            );

        CreateMap<ZaakProcessobjectDto, ZaakProcessobject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore());

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
            .ForMember(
                dest => dest.StartdatumBewaartermijn,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.StartdatumBewaartermijn))
            )
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(
                dest => dest.VertrouwelijkheidAanduiding,
                opt => opt.MapFrom(src => MapVertrouwelijkheidAanduiding(src.Vertrouwelijkheidaanduiding))
            )
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaaktype, opt => opt.MapFrom(src => src.Zaaktype.TrimEnd('/')));

        //
        // 2. ZaakStatus

        CreateMap<GetAllZaakStatussenQueryParameters, GetAllZaakStatussenFilter>()
            .ForMember(
                dest => dest.IndicatieLaatstGezetteStatus,
                opt => opt.MapFrom(src => ProfileHelper.BooleanFromString(src.IndicatieLaatstGezetteStatus))
            );

        CreateMap<ZaakStatusRequestDto, ZaakStatus>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.DatumStatusGezet, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.DatumStatusGezet)))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        //
        // 3. ZaakObjecten

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
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.Object, opt => opt.MapFrom(src => src.Object))
            .ForMember(dest => dest.ZaakObjectType, opt => opt.MapFrom(src => src.ZaakObjectType))
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

        CreateMap<AdresZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Adres, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<BuurtZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Buurt, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<PandZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Pand, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<KadastraleOnroerendeZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.KadastraleOnroerendeZaak, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<GemeenteZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Gemeente, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<TerreinGebouwdObjectZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.TerreinGebouwdObject, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<OverigeZaakObjectRequestDto, ZaakObject>().ForMember(dest => dest.Overige, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        CreateMap<WozWaardeZaakObjectRequestDto, ZaakObject>()
            .ForMember(dest => dest.WozWaardeObject, opt => opt.MapFrom(src => src.ObjectIdentificatie));

        //
        // 4. ZaakInformatieObjecten

        CreateMap<ZaakInformatieObjectRequestDto, ZaakInformatieObject>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RegistratieDatum, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.VernietigingsDatum, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.VernietigingsDatum)));

        //
        // 5. ZaakRol

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
            .ForMember(dest => dest.AfwijkendeNaamBetrokkene, opt => opt.MapFrom(src => src.AfwijkendeNaamBetrokkene))
            .ForMember(dest => dest.RolType, opt => opt.MapFrom(src => src.RolType))
            .ForMember(dest => dest.Roltoelichting, opt => opt.MapFrom(src => src.RolToelichting))
            .ForMember(dest => dest.Registratiedatum, opt => opt.Ignore())
            .ForMember(dest => dest.Omschrijving, opt => opt.Ignore())
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieMachtiging, opt => opt.MapFrom(src => src.IndicatieMachtiging))
            .ForMember(dest => dest.ContactpersoonRolId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactpersoonRol, opt => opt.MapFrom(src => src.ContactpersoonRol))
            .ForMember(dest => dest.NatuurlijkPersoon, opt => opt.Ignore())
            .ForMember(dest => dest.NietNatuurlijkPersoon, opt => opt.Ignore())
            .ForMember(dest => dest.Vestiging, opt => opt.Ignore())
            .ForMember(dest => dest.Medewerker, opt => opt.Ignore())
            .ForMember(dest => dest.OrganisatorischeEenheid, opt => opt.Ignore())
            .IncludeAllDerived();

        CreateMap<ContactpersoonRolDto, ContactpersoonRol>().ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<NatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.NatuurlijkPersoon, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        CreateMap<NietNatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.NietNatuurlijkPersoon, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        // Note: We cannot use the v1.0 mapper because VestigingZaakRolDto contains a new field KvkNummer
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
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<VestigingZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.Vestiging, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie))
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<OrganisatorischeEenheidZaakRolRequestDto, ZaakRol>()
            .ForMember(dest => dest.OrganisatorischeEenheid, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        CreateMap<MedewerkerZaakRolRequestDto, ZaakRol>().ForMember(dest => dest.Medewerker, opt => opt.MapFrom(src => src.BetrokkeneIdentificatie));

        //
        // 10. ZaakVerzoek

        CreateMap<GetAllZaakVerzoekenQueryParameters, GetAllZaakVerzoekenFilter>();

        CreateMap<ZaakVerzoekRequestDto, ZaakVerzoek>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        //
        // 11. ZaakContactmoment

        CreateMap<GetAllZaakContactmomentenQueryParameters, GetAllZaakContactmomentenFilter>();

        CreateMap<ZaakContactmomentRequestDto, ZaakContactmoment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Zaak, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
    }

    private static VertrouwelijkheidAanduiding MapVertrouwelijkheidAanduiding(string vertrouwelijkheidaanduiding)
    {
        return string.IsNullOrWhiteSpace(vertrouwelijkheidaanduiding)
            ? VertrouwelijkheidAanduiding.nullvalue
            : Enum.Parse<VertrouwelijkheidAanduiding>(vertrouwelijkheidaanduiding);
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
