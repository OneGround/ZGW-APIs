using Mapster;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._2;

// Note: This register adds extended mappings (above the ones defined in v1.0)
public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        //
        // 1. This map is used to merge an existing ZaakEigenschap with the PATCH operation

        config.NewConfig<ZaakEigenschap, ZaakEigenschapRequestDto>().Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak));
    }
}
