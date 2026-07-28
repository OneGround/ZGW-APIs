using System.Linq;
using AutoMapper;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<ApplicatieRequestDto, Applicatie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.FutureAutorisaties, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Url, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => src.ClientIds.Select(client => new ApplicatieClient { ClientId = client })));
    }
}
