using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class TerreinGebouwdObjectZaakObjectDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("adresAanduidingGrp")]
    public AdresAanduidingGrpDto AdresAanduidingGrp { get; set; }
}
