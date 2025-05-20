using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Referentielijsten.Web.Models;

public class CommunicatieKanaal : IUrlEntity
{
    public string Url { get; set; }
    public string Naam { get; set; }
    public string Omschrijving { get; set; }
}
