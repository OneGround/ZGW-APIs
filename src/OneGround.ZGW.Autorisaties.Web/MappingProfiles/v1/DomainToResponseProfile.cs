using System.Linq;
using AutoMapper;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<Applicatie, ApplicatieResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => src.ClientIds.Select(client => client.ClientId)));

        CreateMap<Autorisatie, AutorisatieResponseDto>()
            .ForMember(dest => dest.ComponentWeergave, opt => opt.MapFrom(src => GetComponentWeergave(src.Component)));

        // Note: This map is used to merge an existing APPLICATIE with the PATCH operation
        CreateMap<Applicatie, ApplicatieRequestDto>()
            .ForMember(dest => dest.ClientIds, opt => opt.MapFrom(src => src.ClientIds.Select(client => client.ClientId)));
        CreateMap<Autorisatie, AutorisatieRequestDto>();
    }

    private static string GetComponentWeergave(Component component)
    {
        return component switch
        {
            Component.ac => "Autorisatiecomponent",
            Component.nrc => "Notificatierouteringcomponent",
            Component.zrc => "Zaakregistratiecomponent",
            Component.ztc => "Zaaktypecatalogus",
            Component.drc => "Documentregistratiecomponent",
            Component.brc => "Besluitregistratiecomponent",
            _ => null,
        };
    }
}
