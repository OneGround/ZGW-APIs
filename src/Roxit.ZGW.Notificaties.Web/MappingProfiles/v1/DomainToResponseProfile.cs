using AutoMapper;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Notificaties.Contracts.v1;
using Roxit.ZGW.Notificaties.Contracts.v1.Requests;
using Roxit.ZGW.Notificaties.Contracts.v1.Responses;
using Roxit.ZGW.Notificaties.DataModel;
using Roxit.ZGW.Notificaties.Web.Extensions;

namespace Roxit.ZGW.Notificaties.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<Abonnement, AbonnementResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.Auth, opt => opt.MapFrom(src => "<hidden>"))
            .ForMember(dest => dest.Kanalen, opt => opt.MapFrom(src => src.AbonnementKanalen));

        CreateMap<AbonnementKanaal, AbonnementKanaalDto>()
            .ForMember(dest => dest.Naam, opt => opt.MapFrom(src => src.Kanaal.Naam))
            .ForMember(dest => dest.Filters, opt => opt.MapFrom(src => src.FiltersToDictionary()));

        CreateMap<FilterValue, FilterValueDto>();

        CreateMap<Kanaal, KanaalResponseDto>().ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>());

        // Note: This map is used to merge an existing KANAAL/ABONNEMENT with the PATCH operation
        CreateMap<Kanaal, KanaalRequestDto>();
        CreateMap<Abonnement, AbonnementRequestDto>().ForMember(dest => dest.Kanalen, opt => opt.MapFrom(src => src.AbonnementKanalen));
    }
}
