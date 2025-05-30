using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class AdresZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<AdresZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public AdresZaakObjectDto ObjectIdentificatie { get; set; }
}
