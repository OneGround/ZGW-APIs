using System.Linq;
using AutoMapper;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<Applicatie, ApplicatieResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => src.ClientIds.Select(client => client.ClientId)));

        // Note: This map is used to merge an existing APPLICATIE with the PATCH operation
        CreateMap<Applicatie, ApplicatieRequestDto>()
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => src.ClientIds.Select(client => client.ClientId)));
    }
}
