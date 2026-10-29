using System;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

public class ZaakNotitieResponseDto : ZaakNotitieDto
{
    /// <summary>
    /// URL-referentie naar dit object. Dit is de unieke identificatie en locatie van dit object.
    /// </summary>
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    /// <summary>
    /// De datum waarop de notitie is aangemaakt.
    /// </summary>
    [JsonProperty("aanmaakdatum", Order = 7)]
    public DateTime Aanmaakdatum { get; set; }

    /// <summary>
    /// Datum en tijd waarop de notitie voor het laatst is gewijzigd.
    /// </summary>
    [JsonProperty("wijzigingsdatum", Order = 8)]
    public DateTime Wijzigingsdatum { get; set; }
}
