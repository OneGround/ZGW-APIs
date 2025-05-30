using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public abstract class StatusTypeDto
{
    [JsonProperty("omschrijving", Order = 2)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 3)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("statustekst", Order = 4)]
    public string StatusTekst { get; set; }

    [JsonProperty("zaaktype", Order = 5)]
    public string ZaakType { get; set; }

    [JsonProperty("volgnummer", Order = 6)]
    public int VolgNummer { get; set; }

    [JsonProperty("informeren", Order = 8)]
    public bool? Informeren { get; set; }
}
