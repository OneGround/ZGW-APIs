namespace Roxit.ZGW.Common.Web.Authorization;

public class AuthorizationPermission
{
    public string[] Scopes { get; set; }
    public string InformatieObjectType { get; set; }
    public string ZaakType { get; set; }
    public string BesluitType { get; set; }
    public int? MaximumVertrouwelijkheidAanduiding { get; set; }
}
