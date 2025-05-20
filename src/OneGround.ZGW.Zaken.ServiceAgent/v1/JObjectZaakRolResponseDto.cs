using Newtonsoft.Json.Linq;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1;

public class JObjectZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<JObject>
{
    public JObject BetrokkeneIdentificatie { get; set; }

    public NatuurlijkPersoonZaakRolDto NatuurlijkPersoon => BetrokkeneIdentificatie.ToObject<NatuurlijkPersoonZaakRolDto>();

    public NietNatuurlijkPersoonZaakRolDto NietNatuurlijkPersoon => BetrokkeneIdentificatie.ToObject<NietNatuurlijkPersoonZaakRolDto>();

    public MedewerkerZaakRolDto Medewerker => BetrokkeneIdentificatie.ToObject<MedewerkerZaakRolDto>();

    public VestigingZaakRolDto Vestiging => BetrokkeneIdentificatie.ToObject<VestigingZaakRolDto>();

    public OrganisatorischeEenheidZaakRolDto OrganisatorischeEenheid => BetrokkeneIdentificatie.ToObject<OrganisatorischeEenheidZaakRolDto>();
}
