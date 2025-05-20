using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5;

public abstract class ZaakRolDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("betrokkene", Order = 4)]
    public string Betrokkene { get; set; }

    [JsonProperty("betrokkeneType", Order = 5)]
    public string BetrokkeneType { get; set; }

    [JsonProperty("afwijkendeNaamBetrokkene", Order = 6)]
    public string AfwijkendeNaamBetrokkene { get; set; }

    [JsonProperty("roltype", Order = 7)]
    public string RolType { get; set; }

    [JsonProperty("roltoelichting", Order = 8)]
    public string RolToelichting { get; set; }

    [JsonProperty("indicatieMachtiging", Order = 12)]
    public string IndicatieMachtiging { get; set; }

    [JsonProperty("contactpersoonRol", Order = 13)]
    public ContactpersoonRolDto ContactpersoonRol { get; set; }
}
