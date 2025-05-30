using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public abstract class EigenschapDto
{
    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("definitie")]
    public string Definitie { get; set; }

    [JsonProperty("specificatie")]
    public EigenschapSpecificatieDto Specificatie { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("zaaktype")]
    public string ZaakType { get; set; }
}
