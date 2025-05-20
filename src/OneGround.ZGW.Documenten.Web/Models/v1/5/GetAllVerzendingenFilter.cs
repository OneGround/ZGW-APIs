using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Models.v1._5;

public class GetAllVerzendingenFilter
{
    public AardRelatie? AardRelatie { get; set; }
    public string InformatieObject { get; set; }
    public string Betrokkene { get; set; }
}
