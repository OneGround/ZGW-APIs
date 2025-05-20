using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class TerreinGebouwdObjectZaakObjectDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("adresAanduidingGrp")]
    public AdresAanduidingGrpDto AdresAanduidingGrp { get; set; }
}
