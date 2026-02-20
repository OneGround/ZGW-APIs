using AutoMapper;
using Newtonsoft.Json;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectGetResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.Ignore())
            .ForMember(dest => dest.Identificatie, opt => opt.Ignore())
            .ForMember(dest => dest.Bronorganisatie, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.Ignore())
            .ForMember(dest => dest.Titel, opt => opt.Ignore())
            .ForMember(dest => dest.Vertrouwelijkheidaanduiding, opt => opt.Ignore())
            .ForMember(dest => dest.Auteur, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Formaat, opt => opt.Ignore())
            .ForMember(dest => dest.Taal, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsnaam, opt => opt.Ignore())
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.Link, opt => opt.Ignore())
            .ForMember(dest => dest.Beschrijving, opt => opt.Ignore())
            .ForMember(dest => dest.OntvangstDatum, opt => opt.Ignore())
            .ForMember(dest => dest.VerzendDatum, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.Ignore())
            .ForMember(dest => dest.Ondertekening, opt => opt.Ignore())
            .ForMember(dest => dest.Integriteit, opt => opt.Ignore())
            .AfterMap<MapLatestEnkelvoudigInformatieObjectVersieResponse>();

        CreateMap<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectCreateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.OntvangstDatum)))
            .ForMember(dest => dest.BeginRegistratie, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.BeginRegistratie, true)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VerzendDatum)))
            .ForMember(
                dest => dest.Ondertekening,
                opt => opt.MapFrom(src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(src, true))
            )
            .ForMember(
                dest => dest.Integriteit,
                opt => opt.MapFrom(src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(src, true))
            )
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.MapFrom(src => src.InformatieObject.IndicatieGebruiksrecht))
            .ForMember(dest => dest.Locked, opt => opt.MapFrom(src => src.InformatieObject.Locked))
            .ForMember(dest => dest.Inhoud, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObject.InformatieObjectType));

        CreateMap<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectUpdateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.OntvangstDatum)))
            .ForMember(dest => dest.BeginRegistratie, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.BeginRegistratie, true)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VerzendDatum)))
            .ForMember(
                dest => dest.Ondertekening,
                opt => opt.MapFrom(src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(src, true))
            )
            .ForMember(
                dest => dest.Integriteit,
                opt => opt.MapFrom(src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(src, true))
            )
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.MapFrom(src => src.InformatieObject.IndicatieGebruiksrecht))
            .ForMember(dest => dest.Locked, opt => opt.MapFrom(src => src.InformatieObject.Locked))
            .ForMember(dest => dest.Lock, opt => opt.MapFrom(src => src.InformatieObject.Lock))
            .ForMember(dest => dest.Inhoud, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObject.InformatieObjectType));

        // Note: This map is used to merge an existing ENKELVOUDIGINFORMATIEOBJECT(+VERSIE) with the PATCH operation
        CreateMap<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectUpdateRequestDto>()
            .ForMember(dest => dest.Identificatie, opt => opt.Ignore())
            .ForMember(dest => dest.Bronorganisatie, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.Ignore())
            .ForMember(dest => dest.Titel, opt => opt.Ignore())
            .ForMember(dest => dest.Vertrouwelijkheidaanduiding, opt => opt.Ignore())
            .ForMember(dest => dest.Auteur, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Formaat, opt => opt.Ignore())
            .ForMember(dest => dest.Taal, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsnaam, opt => opt.Ignore())
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.Link, opt => opt.Ignore())
            .ForMember(dest => dest.Beschrijving, opt => opt.Ignore())
            .ForMember(dest => dest.OntvangstDatum, opt => opt.Ignore())
            .ForMember(dest => dest.VerzendDatum, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.Ignore())
            .ForMember(dest => dest.Ondertekening, opt => opt.Ignore())
            .ForMember(dest => dest.Integriteit, opt => opt.Ignore())
            .ForMember(dest => dest.Lock, opt => opt.Ignore()) // Note: Don't merge the lock value because we have to validate the value from request and not the one in the database after the merge)
            .AfterMap<MapLatestEnkelvoudigInformatieObjectVersieRequest>();

        CreateMap<ObjectInformatieObject, ObjectInformatieObjectResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(
                dest => dest.InformatieObject,
                opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject)
            );

        CreateMap<GebruiksRecht, GebruiksRechtResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.InformatieObject, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true)));

        // Note: This map is used to merge an existing GEBRUIKSRECHT with the PATCH operation
        CreateMap<GebruiksRecht, GebruiksRechtRequestDto>()
            .ForMember(dest => dest.InformatieObject, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true)));

        CreateMap<AuditTrailRegel, AuditTrailRegelDto>()
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Wijzigingen, opt => opt.MapFrom(src => ConvertWijzigingenToDto(src.Oud, src.Nieuw)))
            .ForMember(dest => dest.AanmaakDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true)));
    }

    private static WijzigingDto ConvertWijzigingenToDto(string oud, string nieuw)
    {
        var result = new WijzigingDto();

        if (!string.IsNullOrEmpty(oud))
        {
            result.Oud = JsonConvert.DeserializeObject(oud, new ZGWJsonSerializerSettings());
        }
        if (!string.IsNullOrEmpty(nieuw))
        {
            result.Nieuw = JsonConvert.DeserializeObject(nieuw, new ZGWJsonSerializerSettings());
        }
        return result;
    }
}
