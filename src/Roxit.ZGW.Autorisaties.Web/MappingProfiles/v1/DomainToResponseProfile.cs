using System.Linq;
using AutoMapper;
using Roxit.ZGW.Autorisaties.Contracts.v1.Requests;
using Roxit.ZGW.Autorisaties.Contracts.v1.Responses;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;

namespace Roxit.ZGW.Autorisaties.Web.MappingProfiles.v1;

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
