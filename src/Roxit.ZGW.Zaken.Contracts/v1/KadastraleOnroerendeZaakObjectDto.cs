using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class KadastraleOnroerendeZaakObjectDto
{
    [JsonProperty("kadastraleIdentificatie")]
    public string KadastraleIdentificatie { get; set; }

    [JsonProperty("kadastraleAanduiding")]
    public string KadastraleAanduiding { get; set; }
}
