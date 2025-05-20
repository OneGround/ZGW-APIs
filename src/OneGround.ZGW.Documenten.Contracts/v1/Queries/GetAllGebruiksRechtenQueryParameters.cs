using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1.Queries;

public class GetAllGebruiksRechtenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het INFORMATIEOBJECT.
    /// </summary>
    [FromQuery(Name = "informatieobject")]
    public string InformatieObject { get; set; }

    /// <summary>
    /// Begindatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn. Doorgaans is de datum van creatie van het informatieobject de startdatum.
    /// </summary>
    [FromQuery(Name = "startdatum__lt")]
    public string Startdatum__lt { get; set; }

    /// <summary>
    /// Begindatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn. Doorgaans is de datum van creatie van het informatieobject de startdatum.
    /// </summary>
    [FromQuery(Name = "startdatum__lte")]
    public string Startdatum__lte { get; set; }

    /// <summary>
    /// Begindatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn. Doorgaans is de datum van creatie van het informatieobject de startdatum.
    /// </summary>
    [FromQuery(Name = "startdatum__gt")]
    public string Startdatum__gt { get; set; }

    /// <summary>
    /// Begindatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn. Doorgaans is de datum van creatie van het informatieobject de startdatum.
    /// </summary>
    [FromQuery(Name = "startdatum__gte")]
    public string Startdatum__gte { get; set; }

    /// <summary>
    /// Einddatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn.
    /// </summary>
    [FromQuery(Name = "einddatumdatum__lt")]
    public string Einddatum__lt { get; set; }

    /// <summary>
    /// Einddatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn.
    /// </summary>
    [FromQuery(Name = "einddatum__lte")]
    public string Einddatum__lte { get; set; }

    /// <summary>
    /// Einddatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn.
    /// </summary>
    [FromQuery(Name = "einddatum__gt")]
    public string Einddatum__gt { get; set; }

    /// <summary>
    /// Einddatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn.
    /// </summary>
    [FromQuery(Name = "einddatum__gte")]
    public string Einddatum__gte { get; set; }
}
