using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._1.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._1;

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
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObject.InformatieObjectType))
            .AfterMap<MapDownloadLink>();

        CreateMap<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectUpdateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.OntvangstDatum)))
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
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObject.InformatieObjectType))
            .AfterMap<MapDownloadLink>();

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
            .AfterMap<MapLatestEnkelvoudigInformatieObjectVersieRequest>();

        CreateMap<BestandsDeel, BestandsDeelResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Lock, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObjectVersie.InformatieObject.Lock));
    }
}
