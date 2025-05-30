using AutoMapper;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Contracts.v1.Requests;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Models.v1;
using Roxit.ZGW.Common.Helpers;

namespace Roxit.ZGW.Besluiten.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllBesluitenQueryParameters, GetAllBesluitenFilter>();

        CreateMap<BesluitRequestDto, Besluit>()
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VerzendDatum)))
            .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.Datum)))
            .ForMember(dest => dest.IngangsDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.IngangsDatum)))
            .ForMember(dest => dest.VervalDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VervalDatum)))
            .ForMember(dest => dest.PublicatieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.PublicatieDatum)))
            .ForMember(
                dest => dest.UiterlijkeReactieDatum,
                opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.UiterlijkeReactieDatum))
            )
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitInformatieObjecten, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakBesluitUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<GetAllBesluitInformatieObjectenQueryParameters, GetAllBesluitInformatieObjectenFilter>();

        CreateMap<BesluitInformatieObjectRequestDto, BesluitInformatieObject>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Besluit, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitId, opt => opt.Ignore())
            .ForMember(dest => dest.Registratiedatum, opt => opt.Ignore())
            .ForMember(dest => dest.AardRelatie, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
    }
}
