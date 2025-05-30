using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class OverigeZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<OverigeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public OverigeZaakObjectDto ObjectIdentificatie { get; set; }
}
