using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class OverigeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<OverigeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public OverigeZaakObjectDto ObjectIdentificatie { get; set; }
}
