using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7;

public abstract class ZaakNotitieDto
{
    /// <summary>
    /// Korte omschrijving of titel van de notitie. Maximaal 255 tekens.
    /// </summary>
    [JsonProperty("onderwerp", Order = 2)]
    public string Onderwerp { get; set; }

    /// <summary>
    /// De volledige inhoud of beschrijving van de notitie.
    /// </summary>
    [JsonProperty("tekst", Order = 3)]
    public string Tekst { get; set; }

    /// <summary>
    /// Naam of identificatie van de auteur van de notitie. Maximaal 255 tekens.
    /// </summary>
    [JsonProperty("aangemaaktDoor", Order = 4)]
    public string AangemaaktDoor { get; set; }

    /// <summary>
    /// Intern mag enkel een medewerker zien, extern mag gezien worden door medewerker en de initiator.
    /// Uitdrukkelijk toegestane waarden: intern, extern.
    /// </summary>
    [JsonProperty("notitieType", Order = 5)]
    public string NotitieType { get; set; }

    /// <summary>
    /// Een notitie kan alleen gewijzigd worden zolang de status concept is.
    /// Uitdrukkelijk toegestane waarden: concept, definitief.
    /// </summary>
    [JsonProperty("status", Order = 6)]
    public string Status { get; set; }

    /// <summary>
    /// URL-referentie naar een ZAAK.
    /// </summary>
    [JsonProperty("gerelateerdAan", Order = 9)]
    public string GerelateerdAan { get; set; }
}
