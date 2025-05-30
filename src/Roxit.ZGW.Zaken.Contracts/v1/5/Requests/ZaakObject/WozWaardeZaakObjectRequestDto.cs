using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class WozWaardeZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<WozWaardeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public WozWaardeZaakObjectDto ObjectIdentificatie { get; set; }
}
