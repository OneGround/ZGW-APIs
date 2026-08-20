using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

/// <remarks>
/// The <c>src.ClientIds == null ? ...</c> guards must stay, and must yield empty rather than null: a
/// null navigation otherwise makes <c>.Select(...)</c> throw. The sibling
/// <see cref="MappingProfiles.v1.RequestToDomainRegister"/> solves the same problem with
/// <c>.Ignore()</c>+<c>.AfterMapping</c> because its destination also closes a type cycle; there is no
/// cycle on this side, so a folded <c>.Map(...)</c> is enough.
/// </remarks>
public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Applicatie, ApplicatieResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ClientIds, src => src.ClientIds == null ? Enumerable.Empty<string>() : src.ClientIds.Select(client => client.ClientId));
        // dest.Autorisaties is intentionally NOT mapped explicitly: Mapster's convention-based nested
        // mapping handles List<Autorisatie> -> List<AutorisatieResponseDto> using this same local config
        // (so the Autorisatie -> AutorisatieResponseDto rule below, incl. ComponentWeergave, applies), and
        // the global EmptyCollectionIfNull transform (in AddZgwMapster) coalesces a null source to empty.

        config.NewConfig<Autorisatie, AutorisatieResponseDto>().Map(dest => dest.ComponentWeergave, src => GetComponentWeergave(src.Component));

        // Note: This map is used to merge an existing APPLICATIE with the PATCH operation
        config
            .NewConfig<Applicatie, ApplicatieRequestDto>()
            .Map(dest => dest.ClientIds, src => src.ClientIds == null ? Enumerable.Empty<string>() : src.ClientIds.Select(client => client.ClientId));

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
