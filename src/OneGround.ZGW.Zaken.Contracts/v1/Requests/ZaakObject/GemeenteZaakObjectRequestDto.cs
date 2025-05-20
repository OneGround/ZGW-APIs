using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

public class GemeenteZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<GemeenteZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public GemeenteZaakObjectDto ObjectIdentificatie { get; set; }
}
