using AutoMapper;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._6.Requests;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._6;

// Note: This Profile adds extended mappings (above the ones defined in v1.0, v1.2, v1.5, ...)
public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        //
        // 1. Map POST Zaak (geometry) search ZaakSearchRequestDto to internal GetAllZakenFilter model

        CreateMap<ZaakSearchRequestTypedDto, Models.v1._5.GetAllZakenFilter>()
            .BeforeMap((src, _) => AssignPropertiesFromBody(src))
            .ForMember(dest => dest.Archiefactiedatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum)))
            .ForMember(dest => dest.Archiefactiedatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt)))
            .ForMember(dest => dest.Archiefactiedatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt)))
            .ForMember(dest => dest.Archiefnominatie__in, opt => opt.MapFrom(src => src.Archiefnominatie__in))
            .ForMember(dest => dest.Archiefstatus__in, opt => opt.MapFrom(src => src.Archiefstatus__in))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum)))
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte)))
            .ForMember(dest => dest.Bronorganisatie__in, opt => opt.MapFrom(src => src.Bronorganisatie__in))
            .ForMember(dest => dest.Uuid__in, opt => opt.MapFrom(src => src.Uuid__in))
            .ForMember(dest => dest.Zaaktype__in, opt => opt.MapFrom(src => src.Zaaktype__in))
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
    }

    private void AssignPropertiesFromBody(ZaakSearchRequestTypedDto src)
    {
        src.ZaakGeometry = GetZaakGeometryOrDefault(src.Body);

        src.Zaaktype = src.Body?.TryGetObject<string>("zaaktype", out var zaaktype) == true ? zaaktype : null;
        src.Uuid__in = src.Body?.TryGetObject<string[]>("uuid__in", out var uuidStrings) == true ? uuidStrings : null;
        src.Identificatie = src.Body?.TryGetObject<string>("identificatie", out var identificatie) == true ? identificatie : null;
        src.Bronorganisatie = src.Body?.TryGetObject<string>("bronorganisatie", out var bronorganisatie) == true ? bronorganisatie : null;
        src.Bronorganisatie__in =
            src.Body?.TryGetObject<string[]>("bronorganisatie__in", out var bronorganisatieStrings) == true ? bronorganisatieStrings : null;
        src.Zaaktype__in = src.Body?.TryGetObject<string[]>("zaaktype__in", out var zaaktypeStrings) == true ? zaaktypeStrings : null;
        src.Archiefnominatie = src.Body?.TryGetObject<string>("archiefnominatie", out var archiefnominatie) == true ? archiefnominatie : null;
        src.Archiefnominatie__in =
            src.Body?.TryGetObject<string[]>("archiefnominatie__in", out var archiefnominatieStrings) == true ? archiefnominatieStrings : null;
        src.Archiefactiedatum = src.Body?.TryGetObject<string>("archiefactiedatum", out var archiefactiedatum) == true ? archiefactiedatum : null;
        src.Archiefactiedatum__isnull =
            src.Body?.TryGetObject<string>("archiefactiedatum__isnull", out var archiefactiedatum__isnull) == true ? archiefactiedatum__isnull : null;
        src.Archiefactiedatum__lt =
            src.Body?.TryGetObject<string>("archiefactiedatum__lt", out var archiefactiedatum__lt) == true ? archiefactiedatum__lt : null;
        src.Archiefactiedatum__gt =
            src.Body?.TryGetObject<string>("archiefactiedatum__gt", out var archiefactiedatum__gt) == true ? archiefactiedatum__gt : null;
        src.Archiefstatus = src.Body?.TryGetObject<string>("archiefstatus", out var archiefstatus) == true ? archiefstatus : null;
        src.Archiefstatus__in =
            src.Body?.TryGetObject<string[]>("archiefstatus__in", out var archiefstatusStrings) == true ? archiefstatusStrings : null;
        src.Startdatum = src.Body?.TryGetObject<string>("startdatum", out var startdatum) == true ? startdatum : null;
        src.Startdatum__gt = src.Body?.TryGetObject<string>("startdatum__gt", out var startdatum__gt) == true ? startdatum__gt : null;
        src.Startdatum__gte = src.Body?.TryGetObject<string>("startdatum__gte", out var startdatum__gte) == true ? startdatum__gte : null;
        src.Startdatum__lt = src.Body?.TryGetObject<string>("startdatum__lt", out var startdatum__lt) == true ? startdatum__lt : null;
        src.Startdatum__lte = src.Body?.TryGetObject<string>("startdatum__lte", out var startdatum__lte) == true ? startdatum__lte : null;
        src.Registratiedatum = src.Body?.TryGetObject<string>("registratiedatum", out var registratiedatum) == true ? registratiedatum : null;
        src.Registratiedatum__gt =
            src.Body?.TryGetObject<string>("registratiedatum__gt", out var registratiedatum__gt) == true ? registratiedatum__gt : null;
        src.Registratiedatum__lt =
            src.Body?.TryGetObject<string>("registratiedatum__lt", out var registratiedatum__lt) == true ? registratiedatum__lt : null;
        src.Einddatum = src.Body?.TryGetObject<string>("einddatum", out var einddatum) == true ? einddatum : null;
        src.Einddatum__isnull = src.Body?.TryGetObject<string>("einddatum__isnull", out var einddatum__isnull) == true ? einddatum__isnull : null;
        src.Einddatum__gt = src.Body?.TryGetObject<string>("einddatum__gt", out var einddatum__gt) == true ? einddatum__gt : null;
        src.Einddatum__lt = src.Body?.TryGetObject<string>("einddatum__lt", out var einddatum__lt) == true ? einddatum__lt : null;
        src.EinddatumGepland = src.Body?.TryGetObject<string>("einddatumGepland", out var einddatumGepland) == true ? einddatumGepland : null;
        src.EinddatumGepland__gt =
            src.Body?.TryGetObject<string>("einddatumGepland__gt", out var einddatumGepland__gt) == true ? einddatumGepland__gt : null;
        src.EinddatumGepland__lt =
            src.Body?.TryGetObject<string>("einddatumGepland__lt", out var einddatumGepland__lt) == true ? einddatumGepland__lt : null;
        src.UiterlijkeEinddatumAfdoening =
            src.Body?.TryGetObject<string>("uiterlijkeEinddatumAfdoening", out var uiterlijkeEinddatumAfdoening) == true
                ? uiterlijkeEinddatumAfdoening
                : null;
        src.UiterlijkeEinddatumAfdoening__gt =
            src.Body?.TryGetObject<string>("uiterlijkeEinddatumAfdoening__gt", out var uiterlijkeEinddatumAfdoening__gt) == true
                ? uiterlijkeEinddatumAfdoening__gt
                : null;
        src.UiterlijkeEinddatumAfdoening__lt =
            src.Body?.TryGetObject<string>("uiterlijkeEinddatumAfdoening__lt", out var uiterlijkeEinddatumAfdoening__lt) == true
                ? uiterlijkeEinddatumAfdoening__lt
                : null;
        src.Rol__betrokkeneType =
            src.Body?.TryGetObject<string>("rol__betrokkeneType", out var rol__betrokkeneType) == true ? rol__betrokkeneType : null;
        src.Rol__betrokkene = src.Body?.TryGetObject<string>("rol__betrokkene", out var rol__betrokkene) == true ? rol__betrokkene : null;
        src.Rol__omschrijvingGeneriek =
            src.Body?.TryGetObject<string>("rol__omschrijvingGeneriek", out var rol__omschrijvingGeneriek) == true ? rol__omschrijvingGeneriek : null;
        src.MaximaleVertrouwelijkheidaanduiding =
            src.Body?.TryGetObject<string>("maximaleVertrouwelijkheidaanduiding", out var maximaleVertrouwelijkheidaanduiding) == true
                ? maximaleVertrouwelijkheidaanduiding
                : null;
        src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn",
                out var rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn
            ) == true
                ? rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn
                : null;
        src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie",
                out var rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie
            ) == true
                ? rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie
                : null;
        src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer",
                out var rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer
            ) == true
                ? rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer
                : null;
        src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId",
                out var rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId
            ) == true
                ? rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId
                : null;
        src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie",
                out var rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie
            ) == true
                ? rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie
                : null;
        src.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__vestiging__vestigingsNummer",
                out var rol__betrokkeneIdentificatie__vestiging__vestigingsNummer
            ) == true
                ? rol__betrokkeneIdentificatie__vestiging__vestigingsNummer
                : null;
        src.Rol__betrokkeneIdentificatie__medewerker__identificatie =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__medewerker__identificatie",
                out var rol__betrokkeneIdentificatie__medewerker__identificatie
            ) == true
                ? rol__betrokkeneIdentificatie__medewerker__identificatie
                : null;
        src.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie =
            src.Body?.TryGetObject<string>(
                "rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie",
                out var rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie
            ) == true
                ? rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie
                : null;
        src.Ordering = src.Body?.TryGetObject<string>("ordering", out var ordering) == true ? ordering : null;
    }

    private WithinGeometry GetZaakGeometryOrDefault(JObject body)
    {
        if (body.TryGetObject<JObject>("zaakgeometrie", out var zaakGeometryObj))
        {
            Geometry geometry = null;
            if (zaakGeometryObj != null)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new[] { new GeometryConverter() },
                };
                geometry = JsonConvert.DeserializeObject<Geometry>(zaakGeometryObj.ToString(), settings);
            }
            return new WithinGeometry { Within = geometry };
        }
        return new WithinGeometry();
    }
}
