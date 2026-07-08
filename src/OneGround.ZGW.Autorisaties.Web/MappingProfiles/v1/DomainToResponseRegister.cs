using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Applicatie, ApplicatieResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ClientIds, src => src.ClientIds.Select(client => client.ClientId));

        config.NewConfig<Autorisatie, AutorisatieResponseDto>().Map(dest => dest.ComponentWeergave, src => GetComponentWeergave(src.Component));

        // Note: This map is used to merge an existing APPLICATIE with the PATCH operation
        config.NewConfig<Applicatie, ApplicatieRequestDto>().Map(dest => dest.ClientIds, src => src.ClientIds.Select(client => client.ClientId));

        config.NewConfig<Autorisatie, AutorisatieRequestDto>();
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
