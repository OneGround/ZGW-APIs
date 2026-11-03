using Mapster;
using OneGround.ZGW.Catalogi.DataModel;
using V3 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._4;

/// <summary>
/// The mapping of the entities to the v1.4 responses. A v1.4 response is the v1.3 response plus <c>_expand</c> (filled by the expand engine, not by
/// the mapping), so each mapping inherits the v1.3 one and only ignores <c>_expand</c>.
/// <para>
/// This is deliberately not an <c>IRegister</c>: <c>Inherits</c> copies the v1.3 mapping at the moment it is called, and the order in which Mapster
/// scans the registers is undefined, so a register of its own could silently inherit nothing. It is registered by <c>ResponseMappingsRegister</c>,
/// right after the v1.3 mapping that it inherits.
/// </para>
/// </summary>
internal static class DomainToResponseMappings
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Catalogus, V4.CatalogusResponseDto>().Inherits<Catalogus, V3.CatalogusResponseDto>().Ignore(dest => dest.Expand);

        config.NewConfig<ZaakType, V4.ZaakTypeResponseDto>().Inherits<ZaakType, V3.ZaakTypeResponseDto>().Ignore(dest => dest.Expand);

        config.NewConfig<BesluitType, V4.BesluitTypeResponseDto>().Inherits<BesluitType, V3.BesluitTypeResponseDto>().Ignore(dest => dest.Expand);

        config
            .NewConfig<InformatieObjectType, V4.InformatieObjectTypeResponseDto>()
            .Inherits<InformatieObjectType, V3.InformatieObjectTypeResponseDto>()
            .Ignore(dest => dest.Expand);

        config.NewConfig<Eigenschap, V4.EigenschapResponseDto>().Inherits<Eigenschap, V3.EigenschapResponseDto>().Ignore(dest => dest.Expand);

        config
            .NewConfig<ResultaatType, V4.ResultaatTypeResponseDto>()
            .Inherits<ResultaatType, V3.ResultaatTypeResponseDto>()
            .Ignore(dest => dest.Expand);

        config.NewConfig<RolType, V4.RolTypeResponseDto>().Inherits<RolType, V3.RolTypeResponseDto>().Ignore(dest => dest.Expand);

        config.NewConfig<StatusType, V4.StatusTypeResponseDto>().Inherits<StatusType, V3.StatusTypeResponseDto>().Ignore(dest => dest.Expand);
    }
}
