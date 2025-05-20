using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Models.v1;

public class GetAllZaakObjectenFilter
{
    public string Zaak { get; set; }
    public string Object { get; set; }
    public ObjectType? ObjectType { get; set; }
}
