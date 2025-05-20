using Newtonsoft.Json;

namespace OneGround.ZGW.Besluiten.Contracts.v1;

public abstract class BesluitDto
{
    [JsonProperty("identificatie", Order = 3)]
    public string Identificatie { get; set; }

    [JsonProperty("verantwoordelijkeOrganisatie", Order = 4)]
    public string VerantwoordelijkeOrganisatie { get; set; }

    [JsonProperty("besluittype", Order = 5)]
    public string BesluitType { get; set; }

    [JsonProperty("zaak", Order = 6)]
    public string Zaak { get; set; }

    [JsonProperty("datum", Order = 7)]
    public string Datum { get; set; }

    [JsonProperty("toelichting", Order = 8)]
    public string Toelichting { get; set; }

    [JsonProperty("bestuursorgaan", Order = 9)]
    public string BestuursOrgaan { get; set; }

    [JsonProperty("ingangsdatum", Order = 10)]
    public string IngangsDatum { get; set; }

    [JsonProperty("vervaldatum", Order = 12)]
    public string VervalDatum { get; set; }

    [JsonProperty("vervalreden", Order = 13)]
    public string VervalReden { get; set; } = "";

    [JsonProperty("publicatiedatum", Order = 14)]
    public string PublicatieDatum { get; set; }

    [JsonProperty("verzenddatum", Order = 15)]
    public string VerzendDatum { get; set; }

    [JsonProperty("uiterlijkeReactiedatum", Order = 16)]
    public string UiterlijkeReactieDatum { get; set; }
}
