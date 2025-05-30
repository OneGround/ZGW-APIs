using AutoMapper;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Models;

namespace Roxit.ZGW.Zaken.Web.MappingProfiles;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<PaginationQuery, PaginationFilter>();
    }
}
