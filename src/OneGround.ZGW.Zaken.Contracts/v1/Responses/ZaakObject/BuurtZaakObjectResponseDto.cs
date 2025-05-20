using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class BuurtZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<BuurtZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public BuurtZaakObjectDto ObjectIdentificatie { get; set; }
}
