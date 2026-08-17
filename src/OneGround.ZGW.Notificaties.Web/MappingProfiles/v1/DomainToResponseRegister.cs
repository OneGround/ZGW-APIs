using Mapster;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.Extensions;

namespace OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Abonnement, AbonnementResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Auth, src => "<hidden>")
            .Map(dest => dest.Kanalen, src => src.AbonnementKanalen);

        config
            .NewConfig<AbonnementKanaal, AbonnementKanaalDto>()
            .Map(dest => dest.Naam, src => src.Kanaal.Naam)
            .Map(dest => dest.Filters, src => src.FiltersToDictionary());

        config.NewConfig<FilterValue, FilterValueDto>();

        config.NewConfig<Kanaal, KanaalResponseDto>().Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src));

        // Note: These maps are used to merge an existing KANAAL/ABONNEMENT with the PATCH operation
        config.NewConfig<Kanaal, KanaalRequestDto>();
        config.NewConfig<Abonnement, AbonnementRequestDto>().Map(dest => dest.Kanalen, src => src.AbonnementKanalen);
    }
}
