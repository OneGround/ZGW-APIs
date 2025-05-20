using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Referentielijsten.Web.Models;

public class Resultaat : IUrlEntity
{
    public string Url { get; set; }
    public string ProcesType { get; set; }
    public int Nummer { get; set; }
    public string VolledigNummer { get; set; }
    public bool Generiek { get; set; }
    public bool Specifiek { get; set; }
    public string Naam { get; set; }
    public string Omschrijving { get; set; }
    public string Herkomst { get; set; }
    public string Waardering { get; set; }
    public string Procestermijn { get; set; }
    public string ProcestermijnWeergave { get; set; }
    public string Bewaartermijn { get; set; }
    public string Toelichting { get; set; }
    public bool AlgemeenBestuurEnInrichtingOrganisatie { get; set; }
    public bool BedrijfsvoeringEnPersoneel { get; set; }
    public bool PubliekeInformatieEnRegistratie { get; set; }
    public bool Burgerzaken { get; set; }
    public bool Veiligheid { get; set; }
    public bool VerkeerEnVervoer { get; set; }
    public bool Economie { get; set; }
    public bool Onderwijs { get; set; }
    public bool SportCultuurEnRecreatie { get; set; }
    public bool SociaalDomein { get; set; }
    public bool VolksgezonheidEnMilieu { get; set; }
    public bool Vhrosv { get; set; }
    public bool HeffenBelastingen { get; set; }
    public bool AlleTaakgebieden { get; set; }
    public string ProcestermijnOpmerking { get; set; }
}
