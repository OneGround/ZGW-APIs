using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Models.v1;

public class GetAllZaakTypeInformatieObjectTypenFilter
{
    public string ZaakType { get; set; }
    public string InformatieObjectType { get; set; }
    public Richting? Richting { get; set; }
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
}
