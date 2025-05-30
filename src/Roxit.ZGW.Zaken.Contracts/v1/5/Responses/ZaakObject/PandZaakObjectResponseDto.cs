using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class PandZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<PandZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public PandZaakObjectDto ObjectIdentificatie { get; set; }
}
