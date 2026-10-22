using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Requests;

public class ReserveZaakIdentificatieRequestDto
{
    /// <summary>
    /// Het RSIN van de Niet-natuurlijk persoon zijnde de organisatie die de zaak heeft gecreeerd. Dit moet een geldig RSIN zijn van 9 nummers
    /// en voldoen aan https://nl.wikipedia.org/wiki/Burgerservicenummer#11-proef
    /// </summary>
    [JsonProperty("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    /// <summary>
    /// Het aantal identificaties om te reserveren.
    /// </summary>
    [JsonProperty("aantal")]
    public int? Aantal { get; set; }
}
