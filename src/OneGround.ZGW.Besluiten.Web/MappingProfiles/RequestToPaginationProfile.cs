using AutoMapper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;

namespace OneGround.ZGW.Besluiten.Web.MappingProfiles;

public class RequestToPaginationProfile : Profile
{
    public RequestToPaginationProfile()
    {
        CreateMap<PaginationQuery, PaginationFilter>();
    }
}
