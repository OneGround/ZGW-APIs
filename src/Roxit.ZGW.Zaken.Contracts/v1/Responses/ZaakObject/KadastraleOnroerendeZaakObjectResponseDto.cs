using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class KadastraleOnroerendeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<KadastraleOnroerendeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public KadastraleOnroerendeZaakObjectDto ObjectIdentificatie { get; set; }
}
