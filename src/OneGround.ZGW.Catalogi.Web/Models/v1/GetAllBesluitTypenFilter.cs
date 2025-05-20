using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Models.v1;

public class GetAllBesluitTypenFilter
{
    public string Catalogus { get; set; }
    public string ZaakType { get; set; }
    public string InformatieObjectType { get; set; }
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public DateOnly? DatumGeldigheid { get; set; }
}
