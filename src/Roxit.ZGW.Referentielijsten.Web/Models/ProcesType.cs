using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Referentielijsten.Web.Models;

public class ProcesType : IUrlEntity
{
    public string Url { get; set; }
    public int Nummer { get; set; }
    public int Jaar { get; set; }
    public string Naam { get; set; }
    public string Omschrijving { get; set; }
    public string Toelichting { get; set; }
    public string ProcesObject { get; set; }
}
