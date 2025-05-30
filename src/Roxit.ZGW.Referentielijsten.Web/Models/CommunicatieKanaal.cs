using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Referentielijsten.Web.Models;

public class CommunicatieKanaal : IUrlEntity
{
    public string Url { get; set; }
    public string Naam { get; set; }
    public string Omschrijving { get; set; }
}
