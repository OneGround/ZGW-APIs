using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class WozWaardeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<WozWaardeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public WozWaardeZaakObjectDto ObjectIdentificatie { get; set; }
}
