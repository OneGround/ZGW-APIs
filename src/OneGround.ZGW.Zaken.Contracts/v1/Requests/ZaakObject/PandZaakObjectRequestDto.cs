using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

public class PandZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<PandZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public PandZaakObjectDto ObjectIdentificatie { get; set; }
}
