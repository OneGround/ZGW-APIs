namespace OneGround.ZGW.Documenten.Web.Models.v1._5;

public class GetAllEnkelvoudigInformatieObjectenFilter
{
    public string Bronorganisatie { get; set; }
    public string Identificatie { get; set; }
    public string[] Uuid_In { get; set; } = null;
    public string[] Trefwoorden_In { get; set; } = null;
}
