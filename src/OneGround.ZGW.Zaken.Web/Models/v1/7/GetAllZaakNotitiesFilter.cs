using System;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Models.v1._7;

public class GetAllZaakNotitiesFilter
{
    public string AangemaaktDoor { get; set; }
    public string AangemaaktDoor__icontains { get; set; }
    public DateTimeWindow Aanmaakdatum { get; set; }
    public DateTime? Aanmaakdatum__gt { get; set; }
    public DateTime? Aanmaakdatum__gte { get; set; }
    public DateTime? Aanmaakdatum__lt { get; set; }
    public DateTime? Aanmaakdatum__lte { get; set; }
    public string GerelateerdAan { get; set; }
    public ZaakNotitieType? NotitieType { get; set; }
    public string Onderwerp { get; set; }
    public string Onderwerp__icontains { get; set; }
    public ZaakNotitieStatus? Status { get; set; }
    public string Tekst { get; set; }
    public string Tekst__icontains { get; set; }
    public DateTimeWindow Wijzigingsdatum { get; set; }
    public DateTime? Wijzigingsdatum__gt { get; set; }
    public DateTime? Wijzigingsdatum__gte { get; set; }
    public DateTime? Wijzigingsdatum__lt { get; set; }
    public DateTime? Wijzigingsdatum__lte { get; set; }
}
