using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Applicatie, ApplicatieResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ClientIds, src => src.ClientIds == null ? Enumerable.Empty<string>() : src.ClientIds.Select(client => client.ClientId));
        // dest.AlleenIsGereedVoorPublicatie (the field v1.1 adds) needs no rule: same name, same type.
        // dest.Autorisaties is intentionally NOT mapped explicitly: v1.1 reuses v1's AutorisatieResponseDto,
        // so Mapster's convention-based nested mapping resolves List<Autorisatie> through the
        // Autorisatie -> AutorisatieResponseDto rule in v1's DomainToResponseRegister (incl.
        // ComponentWeergave) — both registers land in the same scanned config — and the global
        // EmptyCollectionIfNull transform (in AddZgwMapster) coalesces a null source to empty.

        // Note: This map is used to merge an existing APPLICATIE with the PATCH operation
        config
            .NewConfig<Applicatie, ApplicatieRequestDto>()
            .Map(dest => dest.ClientIds, src => src.ClientIds == null ? Enumerable.Empty<string>() : src.ClientIds.Select(client => client.ClientId));
    }
}
