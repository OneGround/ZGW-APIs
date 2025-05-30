using AutoMapper;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;
using Roxit.ZGW.Referentielijsten.Web.Models;

namespace Roxit.ZGW.Referentielijsten.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<ResultaatTypeOmschrijving, ResultaatTypeOmschrijvingResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<AdjustUrl, string>(src => src.Url));
        CreateMap<CommunicatieKanaal, CommunicatieKanaalResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<AdjustUrl, string>(src => src.Url));
        CreateMap<ProcesType, ProcesTypeResponseDto>().ForMember(dest => dest.Url, src => src.MapFrom<AdjustUrl, string>(src => src.Url));
        CreateMap<Resultaat, ResultaatResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<AdjustUrl, string>(src => src.Url))
            .ForMember(dest => dest.ProcesType, opt => opt.MapFrom<AdjustUrl, string>(src => src.ProcesType));
    }
}
