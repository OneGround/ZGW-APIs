using Mapster;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using OneGround.ZGW.Referentielijsten.Web.MappingProfiles;
using OneGround.ZGW.Referentielijsten.Web.Models;

namespace OneGround.ZGW.Referentielijsten.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ResultaatTypeOmschrijving, ResultaatTypeOmschrijvingResponseDto>()
            .Map(dest => dest.Url, src => AdjustUrlMapster.Adjust(src.Url));

        config.NewConfig<CommunicatieKanaal, CommunicatieKanaalResponseDto>().Map(dest => dest.Url, src => AdjustUrlMapster.Adjust(src.Url));

        config.NewConfig<ProcesType, ProcesTypeResponseDto>().Map(dest => dest.Url, src => AdjustUrlMapster.Adjust(src.Url));

        config
            .NewConfig<Resultaat, ResultaatResponseDto>()
            .Map(dest => dest.Url, src => AdjustUrlMapster.Adjust(src.Url))
            .Map(dest => dest.ProcesType, src => AdjustUrlMapster.Adjust(src.ProcesType));
    }
}
