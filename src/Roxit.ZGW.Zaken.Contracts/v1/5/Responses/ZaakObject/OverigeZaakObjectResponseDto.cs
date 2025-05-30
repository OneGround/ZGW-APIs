using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class OverigeZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<OverigeZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public OverigeZaakObjectDto ObjectIdentificatie { get; set; }
}
