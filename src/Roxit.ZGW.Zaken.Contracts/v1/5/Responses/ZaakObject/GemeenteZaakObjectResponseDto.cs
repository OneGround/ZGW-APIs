using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class GemeenteZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<GemeenteZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public GemeenteZaakObjectDto ObjectIdentificatie { get; set; }
}
