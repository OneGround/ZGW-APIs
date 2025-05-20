using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

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

    [JsonProperty("naam", Order = 7)]
    public string Naam { get; set; }

    [JsonProperty("versie", Order = 8)]
    public string Versie { get; set; }

    [JsonProperty("begindatumVersie", Order = 9)]
    public string BegindatumVersie { get; set; }
}
