using System;
using AutoMapper;
using Newtonsoft.Json;
using Roxit.ZGW.Besluiten.Contracts.v1.Requests;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common;
using Roxit.ZGW.Common.Contracts.v1.AuditTrail;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.DataAccess.AuditTrail;

namespace Roxit.ZGW.Besluiten.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<Besluit, BesluitResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Datum)))
            .ForMember(dest => dest.IngangsDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.IngangsDatum)))
            .ForMember(dest => dest.PublicatieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.PublicatieDatum)))
            .ForMember(dest => dest.UiterlijkeReactieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.UiterlijkeReactieDatum)))
            .ForMember(dest => dest.VervalDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VervalDatum)))
            .ForMember(dest => dest.VervalRedenWeergave, opt => opt.MapFrom(src => MapVervalRedenWeergave(src.VervalReden)))
            .AfterMap(
                (src, dest) =>
                {
                    if (src.VervalReden == null)
                        dest.VervalReden = "";
                }
            )
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VerzendDatum)));

        // Note: This map is used to merge an existing BESLUIT with the PATCH operation
        CreateMap<Besluit, BesluitRequestDto>()
            .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.Datum)))
            .ForMember(dest => dest.IngangsDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.IngangsDatum)))
            .ForMember(dest => dest.PublicatieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.PublicatieDatum)))
            .ForMember(dest => dest.UiterlijkeReactieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.UiterlijkeReactieDatum)))
            .ForMember(dest => dest.VervalDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VervalDatum)))
            .ForMember(dest => dest.VervalReden, opt => opt.MapFrom(src => src.VervalReden))
            .AfterMap(
                (src, dest) =>
                {
                    if (src.VervalReden == null)
                        dest.VervalReden = "";
                }
            )
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VerzendDatum)));

        CreateMap<BesluitInformatieObject, BesluitInformatieObjectResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Besluit, opt => opt.MapFrom<MemberUrlResolver, Besluit>(src => src.Besluit));

        // Note: This map is used to merge an existing BESLUITINFORMATIEOBJECT with the PATCH operation
        CreateMap<BesluitInformatieObject, BesluitInformatieObjectRequestDto>()
            .ForMember(dest => dest.Besluit, opt => opt.MapFrom<MemberUrlResolver, Besluit>(src => src.Besluit));

        CreateMap<AuditTrailRegel, AuditTrailRegelDto>()
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Wijzigingen, opt => opt.MapFrom(src => ConvertWijzigingenToDto(src.Oud, src.Nieuw)))
            .ForMember(dest => dest.AanmaakDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true)));
    }

    private static string MapVervalRedenWeergave(VervalReden? vervalReden)
    {
        if (!vervalReden.HasValue)
            return "";

        return vervalReden.Value switch
        {
            VervalReden.tijdelijk => "Besluit met tijdelijke werking",
            VervalReden.ingetrokken_overheid => "Besluit ingetrokken door overheid",
            VervalReden.ingetrokken_belanghebbende => "Besluit ingetrokken o.v.v. belanghebbende",
            _ => throw new InvalidOperationException($"{nameof(VervalReden)} not handled."),
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
