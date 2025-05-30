using System;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Models.v1._3;

public class GetAllBesluitTypenFilter
{
    public string Catalogus { get; set; }
    public string ZaakType { get; set; }
    public string InformatieObjectType { get; set; }
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public DateOnly? DatumGeldigheid { get; set; }
    public string Omschrijving { get; set; }
}
