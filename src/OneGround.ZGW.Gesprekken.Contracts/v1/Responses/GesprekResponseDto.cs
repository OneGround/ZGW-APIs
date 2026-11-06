using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class GesprekResponseDto
{
    [JsonProperty("uuid", Order = 1)]
    public string Uuid { get; set; }

    [JsonProperty("url", Order = 2)]
    public string Url { get; set; }

    [JsonProperty("referentie", Order = 3)]
    public string Referentie { get; set; }

    [JsonProperty("aanvangsmomentGesprek", Order = 4)]
    public string AanvangsmomentGesprek { get; set; }

    [JsonProperty("momentEindeGesprek", Order = 5)]
    public string MomentEindeGesprek { get; set; }

    [JsonProperty("gespreksonderwerp", Order = 6)]
    public string Gespreksonderwerp { get; set; }

    [JsonProperty("aanleidingZaak", Order = 7)]
    public string AanleidingZaak { get; set; }

    [JsonProperty("aanleidingThema", Order = 8)]
    public string AanleidingThema { get; set; }

    [JsonProperty("aanleidingProductDienst", Order = 9)]
    public string AanleidingProductDienst { get; set; }

    [JsonProperty("aanleidingAgendaAfspraak", Order = 10)]
    public string AanleidingAgendaAfspraak { get; set; }

    [JsonProperty("aanleidingPlan", Order = 11)]
    public string AanleidingPlan { get; set; }

    [JsonProperty("contactmoment", Order = 12)]
    public string Contactmoment { get; set; }
}
