using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class KadastraleOnroerendeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<KadastraleOnroerendeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public KadastraleOnroerendeZaakObjectDto ObjectIdentificatie { get; set; }
}
