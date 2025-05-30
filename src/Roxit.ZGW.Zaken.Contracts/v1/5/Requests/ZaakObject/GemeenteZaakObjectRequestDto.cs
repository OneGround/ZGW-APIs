using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class GemeenteZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<GemeenteZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public GemeenteZaakObjectDto ObjectIdentificatie { get; set; }
}
