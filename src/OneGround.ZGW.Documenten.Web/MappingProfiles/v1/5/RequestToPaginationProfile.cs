using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>()
            .ForMember(dest => dest.RegistratieOp, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.RegistratieOp)));

        CreateMap<DownloadEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
    }
}
