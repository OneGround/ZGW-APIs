using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class AdresZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<AdresZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public AdresZaakObjectDto ObjectIdentificatie { get; set; }
}
