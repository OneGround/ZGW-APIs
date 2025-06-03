using AutoMapper;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Documenten.Contracts.v1._5;
using Roxit.ZGW.Documenten.Contracts.v1._5.Requests;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1._5;

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
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.Ignore())
            .ForMember(dest => dest.Trefwoorden, opt => opt.Ignore())
            .AfterMap<MapLatestEnkelvoudigInformatieObjectVersieResponse>();

        CreateMap<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectCreateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.EnkelvoudigInformatieObject))
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
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.IndicatieGebruiksrecht))
            .ForMember(dest => dest.Locked, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.Locked))
            .ForMember(dest => dest.Lock, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.Lock))
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.MapFrom(src => src.Verschijningsvorm))
            .ForMember(dest => dest.Trefwoorden, opt => opt.MapFrom(src => src.Trefwoorden))
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.InformatieObjectType))
            // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.MapFrom(src => src.LatestEnkelvoudigInformatieObject.Id))
            .AfterMap<MapDownloadLink>();

        CreateMap<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectUpdateResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.EnkelvoudigInformatieObject))
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
            .ForMember(dest => dest.IndicatieGebruiksrecht, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.IndicatieGebruiksrecht))
            .ForMember(dest => dest.Locked, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.Locked))
            .ForMember(dest => dest.Lock, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.Lock))
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.MapFrom(src => src.Verschijningsvorm))
            .ForMember(dest => dest.Trefwoorden, opt => opt.MapFrom(src => src.Trefwoorden))
            .ForMember(dest => dest.Inhoud, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.EnkelvoudigInformatieObject.InformatieObjectType))
            // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.MapFrom(src => src.LatestEnkelvoudigInformatieObject.Id))
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

        CreateMap<BinnenlandsCorrespondentieAdres, BinnenlandsCorrespondentieAdresDto>()
            .ForMember(dest => dest.Huisletter, opt => opt.MapFrom(src => src.Huisletter))
            .ForMember(dest => dest.Huisnummer, opt => opt.MapFrom(src => src.Huisnummer))
            .ForMember(dest => dest.HuisnummerToevoeging, opt => opt.MapFrom(src => src.HuisnummerToevoeging))
            .ForMember(dest => dest.NaamOpenbareRuimte, opt => opt.MapFrom(src => src.NaamOpenbareRuimte))
            .ForMember(dest => dest.Postcode, opt => opt.MapFrom(src => src.Postcode))
            .ForMember(dest => dest.WoonplaatsNaam, opt => opt.MapFrom(src => src.WoonplaatsNaam));

        CreateMap<BuitenlandsCorrespondentieAdres, BuitenlandsCorrespondentieAdresDto>()
            .ForMember(dest => dest.AdresBuitenland1, opt => opt.MapFrom(src => src.AdresBuitenland1))
            .ForMember(dest => dest.AdresBuitenland2, opt => opt.MapFrom(src => src.AdresBuitenland2))
            .ForMember(dest => dest.AdresBuitenland3, opt => opt.MapFrom(src => src.AdresBuitenland3))
            .ForMember(dest => dest.LandPostadres, opt => opt.MapFrom(src => src.LandPostadres));

        CreateMap<CorrespondentiePostadres, CorrespondentiePostAdresDto>()
            .ForMember(dest => dest.PostbusOfAntwoordnummer, opt => opt.MapFrom(src => src.PostbusOfAntwoordnummer))
            .ForMember(dest => dest.PostadresPostcode, opt => opt.MapFrom(src => src.PostadresPostcode))
            .ForMember(dest => dest.PostadresType, opt => opt.MapFrom(src => src.PostadresType))
            .ForMember(dest => dest.WoonplaatsNaam, opt => opt.MapFrom(src => src.WoonplaatsNaam));

        CreateMap<Verzending, VerzendingResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.InformatieObject, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.Betrokkene, opt => opt.MapFrom(src => src.Betrokkene))
            .ForMember(dest => dest.AardRelatie, opt => opt.MapFrom(src => src.AardRelatie.ToString()))
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => src.Toelichting))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Ontvangstdatum)))
            .ForMember(dest => dest.Verzenddatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Verzenddatum)))
            .ForMember(dest => dest.Contactpersoon, opt => opt.MapFrom(src => src.Contactpersoon))
            .ForMember(dest => dest.BinnenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BinnenlandsCorrespondentieAdres))
            .ForMember(dest => dest.BuitenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BuitenlandsCorrespondentieAdres))
            .ForMember(dest => dest.CorrespondentiePostadres, opt => opt.MapFrom(src => src.CorrespondentiePostadres))
            .ForMember(dest => dest.Faxnummer, opt => opt.MapFrom(src => src.Faxnummer))
            .ForMember(dest => dest.EmailAdres, opt => opt.MapFrom(src => src.EmailAdres))
            .ForMember(dest => dest.MijnOverheid, opt => opt.MapFrom(src => src.MijnOverheid))
            .ForMember(dest => dest.Telefoonnummer, opt => opt.MapFrom(src => src.Telefoonnummer))
            .AfterMap(
                (_, dest) =>
                {
                    dest.BinnenlandsCorrespondentieAdres ??= new BinnenlandsCorrespondentieAdresDto();
                    dest.BuitenlandsCorrespondentieAdres ??= new BuitenlandsCorrespondentieAdresDto();
                    dest.CorrespondentiePostadres ??= new CorrespondentiePostAdresDto();
                }
            );

        // Note: This map is used to merge an existing VERZENDING with the PATCH operation
        CreateMap<Verzending, VerzendingRequestDto>()
            .ForMember(dest => dest.InformatieObject, opt => opt.MapFrom<MemberUrlResolver, EnkelvoudigInformatieObject>(src => src.InformatieObject))
            .ForMember(dest => dest.Betrokkene, opt => opt.MapFrom(src => src.Betrokkene))
            .ForMember(dest => dest.AardRelatie, opt => opt.MapFrom(src => src.AardRelatie.ToString()))
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => src.Toelichting))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Ontvangstdatum)))
            .ForMember(dest => dest.Verzenddatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Verzenddatum)))
            .ForMember(dest => dest.Contactpersoon, opt => opt.MapFrom(src => src.Contactpersoon))
            .ForMember(dest => dest.BinnenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BinnenlandsCorrespondentieAdres))
            .ForMember(dest => dest.BuitenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BuitenlandsCorrespondentieAdres))
            .ForMember(dest => dest.CorrespondentiePostadres, opt => opt.MapFrom(src => src.CorrespondentiePostadres))
            .ForMember(dest => dest.Faxnummer, opt => opt.MapFrom(src => src.Faxnummer))
            .ForMember(dest => dest.EmailAdres, opt => opt.MapFrom(src => src.EmailAdres))
            .ForMember(dest => dest.MijnOverheid, opt => opt.MapFrom(src => src.MijnOverheid))
            .ForMember(dest => dest.Telefoonnummer, opt => opt.MapFrom(src => src.Telefoonnummer));
    }
}
