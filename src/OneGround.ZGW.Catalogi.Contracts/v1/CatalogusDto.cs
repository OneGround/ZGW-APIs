using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class CatalogusDto
{
    [JsonProperty("domein", Order = 2)]
    public string Domein { get; set; }

    [JsonProperty("rsin", Order = 3)]
    public string Rsin { get; set; }

    [JsonProperty("contactpersoonBeheerNaam", Order = 4)]
    public string ContactpersoonBeheerNaam { get; set; }

    [JsonProperty("contactpersoonBeheerTelefoonnummer", Order = 5)]
    public string ContactpersoonBeheerTelefoonnummer { get; set; }

    [JsonProperty("contactpersoonBeheerEmailadres", Order = 6)]
    public string ContactpersoonBeheerEmailadres { get; set; }
}
