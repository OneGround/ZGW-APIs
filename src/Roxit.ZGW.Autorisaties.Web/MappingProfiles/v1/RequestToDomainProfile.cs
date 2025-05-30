using System.Linq;
using AutoMapper;
using Roxit.ZGW.Autorisaties.Contracts.v1.Requests;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;
using Roxit.ZGW.Autorisaties.Web.Models;
using Roxit.ZGW.Common.Helpers;

namespace Roxit.ZGW.Autorisaties.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllApplicatiesQueryParameters, GetAllApplicatiesFilter>()
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.ClientIds)));

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

        CreateMap<AutorisatieRequestDto, Autorisatie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Applicatie, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicatieId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());
    }
}
