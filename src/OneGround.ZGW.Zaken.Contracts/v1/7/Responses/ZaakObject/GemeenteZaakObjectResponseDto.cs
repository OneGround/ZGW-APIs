using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;

public class GemeenteZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<GemeenteZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public GemeenteZaakObjectDto ObjectIdentificatie { get; set; }
}
