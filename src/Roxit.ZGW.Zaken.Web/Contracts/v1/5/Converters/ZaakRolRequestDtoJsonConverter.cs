using System;
using Roxit.ZGW.Common.JsonConverters;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Contracts.v1._5.Converters;

public class ZaakRolRequestDtoJsonConverter : DerivedTypeJsonConverter<ZaakRolRequestDto>
{
    protected override string TypePropertyName => "betrokkeneType";

    protected override Type NameToType(string typeName)
    {
        if (!Enum.TryParse<BetrokkeneType>(typeName, ignoreCase: true, out var betrokkeneType))
        {
            return typeof(InvalidZaakRolRequestDto);
        }

        return betrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => typeof(NatuurlijkPersoonZaakRolRequestDto),
            BetrokkeneType.niet_natuurlijk_persoon => typeof(NietNatuurlijkPersoonZaakRolRequestDto),
            BetrokkeneType.vestiging => typeof(VestigingZaakRolRequestDto),
            BetrokkeneType.organisatorische_eenheid => typeof(OrganisatorischeEenheidZaakRolRequestDto),
            BetrokkeneType.medewerker => typeof(MedewerkerZaakRolRequestDto),

            _ => typeof(InvalidZaakRolRequestDto),
        };
    }
}
