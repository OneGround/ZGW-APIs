using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class WozWaardeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<WozWaardeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public WozWaardeZaakObjectDto ObjectIdentificatie { get; set; }
}
