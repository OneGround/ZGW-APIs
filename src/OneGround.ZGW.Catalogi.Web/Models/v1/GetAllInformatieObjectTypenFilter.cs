using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Models.v1;

public class GetAllInformatieObjectTypenFilter
{
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public string Catalogus { get; set; }
    public DateOnly? DatumGeldigheid { get; set; }
    public string Omschrijving { get; set; }
}
