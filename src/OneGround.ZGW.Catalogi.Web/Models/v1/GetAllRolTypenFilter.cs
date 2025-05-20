using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Models.v1;

public class GetAllRolTypenFilter
{
    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;
    public string ZaakType { get; set; }
    public Common.DataModel.OmschrijvingGeneriek? OmschrijvingGeneriek { get; set; }
}
