using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class BuurtZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<BuurtZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public BuurtZaakObjectDto ObjectIdentificatie { get; set; }
}
