using AutoMapper;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
        CreateMap<DownloadEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
    }
}
