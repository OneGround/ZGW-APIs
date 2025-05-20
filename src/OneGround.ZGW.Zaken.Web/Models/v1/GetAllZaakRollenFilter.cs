using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Models.v1;

public class GetAllZaakRollenFilter
{
    public string Zaak { get; set; }
    public string Betrokkene { get; set; }
    public BetrokkeneType? BetrokkeneType { get; set; }
    public string NatuurlijkPersoonInpBsn { get; set; }
    public string NatuurlijkPersoonAnpIdentificatie { get; set; }
    public string NatuurlijkPersoonInpANummer { get; set; }
    public string NietNatuurlijkPersoonInnNnpId { get; set; }
    public string NietNatuurlijkPersoonAnnIdentificatie { get; set; }
    public string VestigingNummer { get; set; }
    public string OrganisatorischeEenheidIdentificatie { get; set; }
    public string MedewerkerIdentificatie { get; set; }
    public string RolType { get; set; }
    public string Omschrijving { get; set; }
    public OmschrijvingGeneriek? OmschrijvingGeneriek { get; set; }
}
