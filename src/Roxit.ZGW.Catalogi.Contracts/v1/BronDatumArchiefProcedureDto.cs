using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public class BronDatumArchiefProcedureDto
{
    [JsonProperty("afleidingswijze")]
    public string Afleidingswijze { get; set; }

    [JsonProperty("datumkenmerk")]
    public string DatumKenmerk { get; set; }

    [JsonProperty("einddatumBekend")]
    public bool? EindDatumBekend { get; set; }

    [JsonProperty("objecttype")]
    public string ObjectType { get; set; }

    [JsonProperty("registratie")]
    public string Registratie { get; set; }

    [JsonProperty("procestermijn")]
    public string ProcesTermijn { get; set; }
}
