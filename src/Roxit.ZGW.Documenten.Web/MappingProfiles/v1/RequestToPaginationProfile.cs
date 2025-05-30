using AutoMapper;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;
using Roxit.ZGW.Documenten.Web.Models.v1;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<PaginationQuery, PaginationFilter>();

        CreateMap<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>();
    }
}
