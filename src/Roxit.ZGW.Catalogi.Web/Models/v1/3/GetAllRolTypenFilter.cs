using System;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Models.v1._3;

public class GetAllRolTypenFilter
{
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public string ZaakType { get; set; }
    public Common.DataModel.OmschrijvingGeneriek? OmschrijvingGeneriek { get; set; }
    public string ZaaktypeIdentificatie { get; set; }
    public DateOnly? DatumGeldigheid { get; set; }
}
