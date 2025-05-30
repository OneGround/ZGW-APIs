using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class KadastraleOnroerendeZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<KadastraleOnroerendeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public KadastraleOnroerendeZaakObjectDto ObjectIdentificatie { get; set; }
}
