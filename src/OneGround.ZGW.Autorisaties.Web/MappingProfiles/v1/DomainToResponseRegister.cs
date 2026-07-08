using System.Collections.Generic;
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
            .Map(dest => dest.ClientIds, src => src.ClientIds.Select(client => client.ClientId))
            // AutoMapper's default (AllowNullCollections = false) substitutes an empty collection for a null
            // source collection. Mapster has no such default and passes null through, so it must be made explicit
            // here to keep parity with the AutoMapper baseline. (Originally caught and proven by the temporary
            // AutoMapper-vs-Mapster parity test for this service, deleted once AC's migration completed.)
            //
            // `config` must be passed explicitly here — a bare `.Adapt<AutorisatieResponseDto>()` (no config
            // argument) would resolve against Mapster's ambient TypeAdapterConfig.GlobalSettings instead of
            // this local config, silently dropping the ComponentWeergave rule registered below. See
            // MapsterSeamHealthTests.Bare_Adapt_call_does_not_see_a_locally_built_configs_custom_rule.
            .Map(
                dest => dest.Autorisaties,
                src => (src.Autorisaties ?? new List<Autorisatie>()).Select(a => a.Adapt<Autorisatie, AutorisatieResponseDto>(config)).ToList()
            );

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
