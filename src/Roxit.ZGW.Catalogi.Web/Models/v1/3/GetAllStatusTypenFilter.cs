using System;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Models.v1._3;

public class GetAllStatusTypenFilter
{
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public string ZaakType { get; set; }
    public string ZaaktypeIdentificatie { get; set; }
    public DateOnly? DatumGeldigheid { get; set; }
}
