using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class KadastraleOnroerendeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<KadastraleOnroerendeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public KadastraleOnroerendeZaakObjectDto ObjectIdentificatie { get; set; }
}
