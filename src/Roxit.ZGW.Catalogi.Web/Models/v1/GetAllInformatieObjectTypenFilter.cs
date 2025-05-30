using System;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Models.v1;

public class GetAllInformatieObjectTypenFilter
{
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public string Catalogus { get; set; }
    public DateOnly? DatumGeldigheid { get; set; }
    public string Omschrijving { get; set; }
}
