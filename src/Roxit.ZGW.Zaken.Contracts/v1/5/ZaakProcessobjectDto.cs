using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public class ZaakProcessobjectDto
{
    [JsonProperty("datumkenmerk", Order = 1)]
    public string Datumkenmerk { get; set; }

    [JsonProperty("identificatie", Order = 2)]
    public string Identificatie { get; set; }

    [JsonProperty("objecttype", Order = 3)]
    public string Objecttype { get; set; }

    [JsonProperty("registratie", Order = 4)]
    public string Registratie { get; set; }
}
