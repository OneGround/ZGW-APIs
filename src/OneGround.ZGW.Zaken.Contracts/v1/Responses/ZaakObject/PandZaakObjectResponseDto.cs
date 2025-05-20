using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class PandZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<PandZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public PandZaakObjectDto ObjectIdentificatie { get; set; }
}
