using System;

namespace OneGround.ZGW.Documenten.Web.Models.v1;

public class GetAllGebruiksRechtenFilter
{
    public string InformatieObject { get; set; } // URL-referentie naar het INFORMATIEOBJECT.
    public DateTime? Startdatum__lt { get; set; } // Begindatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn. Doorgaans is de datum van creatie van het informatieobject de startdatum.
    public DateTime? Startdatum__lte { get; set; }
    public DateTime? Startdatum__gt { get; set; }
    public DateTime? Startdatum__gte { get; set; }
    public DateTime? Einddatum__lt { get; set; } // Einddatum van de periode waarin de gebruiksrechtvoorwaarden van toepassing zijn.
    public DateTime? Einddatum__lte { get; set; }
    public DateTime? Einddatum__gt { get; set; }
    public DateTime? Einddatum__gte { get; set; }
}
