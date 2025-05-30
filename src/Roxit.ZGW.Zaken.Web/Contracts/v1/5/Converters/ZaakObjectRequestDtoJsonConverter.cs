using System;
using Roxit.ZGW.Common.JsonConverters;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Contracts.v1._5.Converters;

public class ZaakObjectRequestDtoJsonConverter : DerivedTypeJsonConverter<ZaakObjectRequestDto>
{
    protected override string TypePropertyName => "objecttype";

    protected override Type NameToType(string typeName)
    {
        if (!Enum.TryParse<ObjectType>(typeName, ignoreCase: true, out var objectType))
        {
            return typeof(InvalidZaakObjectRequestDto);
        }

        return objectType switch
        {
            ObjectType.adres => typeof(AdresZaakObjectRequestDto),
            ObjectType.buurt => typeof(BuurtZaakObjectRequestDto),
            ObjectType.enkelvoudig_document => typeof(ZaakObjectRequestDto),
            ObjectType.pand => typeof(PandZaakObjectRequestDto),
            ObjectType.kadastrale_onroerende_zaak => typeof(KadastraleOnroerendeZaakObjectRequestDto),
            ObjectType.besluit => typeof(ZaakObjectRequestDto),
            ObjectType.status => typeof(ZaakObjectRequestDto),
            ObjectType.gemeente => typeof(GemeenteZaakObjectRequestDto),
            ObjectType.terrein_gebouwd_object => typeof(TerreinGebouwdObjectZaakObjectRequestDto),
            ObjectType.overige => typeof(OverigeZaakObjectRequestDto),
            ObjectType.woz_waarde => typeof(WozWaardeZaakObjectRequestDto),

            _ => typeof(InvalidZaakObjectRequestDto),
        };
    }
}
