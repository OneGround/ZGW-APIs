using AutoMapper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<PaginationQuery, PaginationFilter>();

        CreateMap<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
    }
}
