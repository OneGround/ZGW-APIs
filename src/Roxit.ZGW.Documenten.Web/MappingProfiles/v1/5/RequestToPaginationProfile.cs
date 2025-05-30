using AutoMapper;
using Roxit.ZGW.Documenten.Contracts.v1._5.Queries;
using Roxit.ZGW.Documenten.Web.Models.v1;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
        CreateMap<DownloadEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
    }
}
