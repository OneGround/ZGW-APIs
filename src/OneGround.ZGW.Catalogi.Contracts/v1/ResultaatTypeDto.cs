using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class ResultaatTypeDto
{
    [JsonProperty("zaaktype")]
    public string ZaakType { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }

    [JsonProperty("resultaattypeomschrijving")]
    public string ResultaatTypeOmschrijving { get; set; }

    [JsonProperty("selectielijstklasse")]
    public string SelectieLijstKlasse { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("archiefnominatie")]
    public string ArchiefNominatie { get; set; }

    [JsonProperty("archiefactietermijn")]
    public string ArchiefActieTermijn { get; set; }

    [JsonProperty("brondatumArchiefprocedure")]
    public BronDatumArchiefProcedureDto BronDatumArchiefProcedure { get; set; }
}
