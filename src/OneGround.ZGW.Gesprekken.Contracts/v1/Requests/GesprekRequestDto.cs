using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

/// <summary>
/// Maakt een gesprek aan, samen met de ontvanger(s) en de eerste gespreksbijdrage.
/// </summary>
public class GesprekRequestDto
{
    [JsonProperty("gespreksonderwerp", Order = 1)]
    public string Gespreksonderwerp { get; set; }

    [JsonProperty("aanleidingZaak", Order = 2)]
    public string AanleidingZaak { get; set; }

    [JsonProperty("aanleidingThema", Order = 3)]
    public string AanleidingThema { get; set; }

    [JsonProperty("aanleidingProductDienst", Order = 4)]
    public string AanleidingProductDienst { get; set; }

    [JsonProperty("aanleidingAgendaAfspraak", Order = 5)]
    public string AanleidingAgendaAfspraak { get; set; }

    [JsonProperty("aanleidingPlan", Order = 6)]
    public string AanleidingPlan { get; set; }

    [JsonProperty("ontvanger", Order = 7)]
    public List<DeelnameRequestDto> Ontvanger { get; set; } = [];

    [JsonProperty("eersteBijdrage", Order = 8)]
    public EersteGespreksbijdrageRequestDto EersteBijdrage { get; set; }
}
