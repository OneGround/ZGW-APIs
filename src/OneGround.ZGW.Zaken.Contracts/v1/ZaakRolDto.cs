using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class ZaakRolDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("betrokkene", Order = 4)]
    public string Betrokkene { get; set; }

    [JsonProperty("betrokkeneType", Order = 5)]
    public string BetrokkeneType { get; set; }

    [JsonProperty("roltype", Order = 6)]
    public string RolType { get; set; }

    [JsonProperty("roltoelichting", Order = 9)]
    public string RolToelichting { get; set; }

    [JsonProperty("indicatieMachtiging", Order = 11)]
    public string IndicatieMachtiging { get; set; }
}
