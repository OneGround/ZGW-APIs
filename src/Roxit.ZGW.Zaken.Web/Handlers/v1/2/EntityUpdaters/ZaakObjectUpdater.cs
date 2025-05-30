using System;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;

public class ZaakObjectUpdater : IEntityUpdater<ZaakObject>
{
    // Note: Updating of zaakobjecten supported in >= v1.2
    public void Update(ZaakObject request, ZaakObject source, decimal version = 1)
    {
        // Note: It is not allowed to modify attributes: zaak, object and objectType
        source.ObjectTypeOverige = request.ObjectTypeOverige;
        source.RelatieOmschrijving = request.RelatieOmschrijving;

        UpdateObjectTypeOverigeDefinitie(request, source);

        switch (source.ObjectType)
        {
            case ObjectType.adres:
                UpdateAdresZaakObject(request, source);
                break;

            case ObjectType.buurt:
                UpdateBuurtZaakObject(request, source);
                break;

            case ObjectType.gemeente:
                UpdateGemeenteZaakObject(request, source);
                break;

            case ObjectType.kadastrale_onroerende_zaak:
                UpdateKadastraleOnroerendeZaakZaakObject(request, source);
                break;

            case ObjectType.overige:
                UpdateOverigeZaakObject(request, source);
                break;

            case ObjectType.pand:
                UpdatePandZaakObject(request, source);
                break;

            case ObjectType.terrein_gebouwd_object:
                UpdateTerreinGebouwdObjectZaakObject(request, source);
                break;

            case ObjectType.woz_waarde:
                UpdateWozWaardeZaakObject(request, source);
                break;

            // Note: Not (yet) implemented ObjectTypes or not relevant (no details to update)
        }

        // Note: Fields for v1.5 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.5M)
        {
            source.ZaakObjectType = request.ZaakObjectType;
        }
    }

    private static void UpdateObjectTypeOverigeDefinitie(ZaakObject request, ZaakObject source)
    {
        if (request.ObjectTypeOverigeDefinitie == null)
        {
            if (source.ObjectTypeOverigeDefinitie != null)
            {
                source.ObjectTypeOverigeDefinitie = null;
            }
        }
        else
        {
            source.ObjectTypeOverigeDefinitie ??= new ObjectTypeOverigeDefinitie();
            source.ObjectTypeOverigeDefinitie.Url = request.ObjectTypeOverigeDefinitie.Url;
            source.ObjectTypeOverigeDefinitie.Schema = request.ObjectTypeOverigeDefinitie.Schema;
            source.ObjectTypeOverigeDefinitie.ObjectData = request.ObjectTypeOverigeDefinitie.ObjectData;
        }
    }

    private static void UpdateAdresZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.Adres == null)
            throw new NullReferenceException(nameof(request.Adres));
        if (source.Adres == null)
            throw new NullReferenceException(nameof(source.Adres));

        source.Adres.Identificatie = request.Adres.Identificatie;
        source.Adres.Postcode = request.Adres.Postcode;
        source.Adres.WplWoonplaatsNaam = request.Adres.WplWoonplaatsNaam;
        source.Adres.HuisnummerToevoeging = request.Adres.HuisnummerToevoeging;
        source.Adres.GorOpenbareRuimteNaam = request.Adres.GorOpenbareRuimteNaam;
        source.Adres.Huisletter = request.Adres.Huisletter;
        source.Adres.Huisnummer = request.Adres.Huisnummer;
    }

    private static void UpdateBuurtZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.Buurt == null)
            throw new NullReferenceException(nameof(request.Buurt));
        if (source.Buurt == null)
            throw new NullReferenceException(nameof(source.Buurt));

        source.Buurt.BuurtCode = request.Buurt.BuurtCode;
        source.Buurt.BuurtNaam = request.Buurt.BuurtNaam;
        source.Buurt.GemGemeenteCode = request.Buurt.GemGemeenteCode;
        source.Buurt.WykWijkCode = request.Buurt.WykWijkCode;
    }

    private static void UpdateGemeenteZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.Gemeente == null)
            throw new NullReferenceException(nameof(request.Gemeente));
        if (source.Gemeente == null)
            throw new NullReferenceException(nameof(source.Gemeente));

        source.Gemeente.GemeenteCode = request.Gemeente.GemeenteCode;
        source.Gemeente.GemeenteNaam = request.Gemeente.GemeenteNaam;
    }

    private static void UpdateKadastraleOnroerendeZaakZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.KadastraleOnroerendeZaak == null)
            throw new NullReferenceException(nameof(request.KadastraleOnroerendeZaak));
        if (source.KadastraleOnroerendeZaak == null)
            throw new NullReferenceException(nameof(source.KadastraleOnroerendeZaak));

        source.KadastraleOnroerendeZaak.KadastraleAanduiding = request.KadastraleOnroerendeZaak.KadastraleAanduiding;
        source.KadastraleOnroerendeZaak.KadastraleIdentificatie = request.KadastraleOnroerendeZaak.KadastraleIdentificatie;
    }

    private static void UpdateOverigeZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.Overige == null)
            throw new NullReferenceException(nameof(request.Overige));
        if (source.Overige == null)
            throw new NullReferenceException(nameof(source.Overige));

        source.Overige.OverigeData = request.Overige.OverigeData;
    }

    private static void UpdatePandZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.Pand == null)
            throw new NullReferenceException(nameof(request.Pand));
        if (source.Pand == null)
            throw new NullReferenceException(nameof(source.Pand));

        source.Pand.Identificatie = request.Pand.Identificatie;
    }

    private static void UpdateTerreinGebouwdObjectZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.TerreinGebouwdObject == null)
            throw new NullReferenceException(nameof(request.TerreinGebouwdObject));
        if (source.TerreinGebouwdObject == null)
            throw new NullReferenceException(nameof(source.TerreinGebouwdObject));

        source.TerreinGebouwdObject.Identificatie = request.TerreinGebouwdObject.Identificatie;
        source.TerreinGebouwdObject.AdresAanduidingGrp_NumIdentificatie = request.TerreinGebouwdObject.AdresAanduidingGrp_NumIdentificatie;
        source.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie = request.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie;
        source.TerreinGebouwdObject.AdresAanduidingGrp_WplWoonplaatsNaam = request.TerreinGebouwdObject.AdresAanduidingGrp_WplWoonplaatsNaam;
        source.TerreinGebouwdObject.AdresAanduidingGrp_GorOpenbareRuimteNaam = request.TerreinGebouwdObject.AdresAanduidingGrp_GorOpenbareRuimteNaam;
        source.TerreinGebouwdObject.AdresAanduidingGrp_AoaPostcode = request.TerreinGebouwdObject.AdresAanduidingGrp_AoaPostcode;
        source.TerreinGebouwdObject.AdresAanduidingGrp_AoaHuisnummer = request.TerreinGebouwdObject.AdresAanduidingGrp_AoaHuisnummer;
        source.TerreinGebouwdObject.AdresAanduidingGrp_AoaHuisletter = request.TerreinGebouwdObject.AdresAanduidingGrp_AoaHuisletter;
        source.TerreinGebouwdObject.AdresAanduidingGrp_AoaHuisnummertoevoeging = request
            .TerreinGebouwdObject
            .AdresAanduidingGrp_AoaHuisnummertoevoeging;
        source.TerreinGebouwdObject.AdresAanduidingGrp_OgoLocatieAanduiding = request.TerreinGebouwdObject.AdresAanduidingGrp_OgoLocatieAanduiding;
    }

    private static void UpdateWozWaardeZaakObject(ZaakObject request, ZaakObject source)
    {
        if (request.WozWaardeObject == null)
            throw new NullReferenceException(nameof(request.WozWaardeObject));
        if (source.WozWaardeObject == null)
            throw new NullReferenceException(nameof(source.WozWaardeObject));

        source.WozWaardeObject.WaardePeildatum = request.WozWaardeObject.WaardePeildatum;

        // Update/remove/add Child object
        if (request.WozWaardeObject.IsVoor != null)
        {
            if (source.WozWaardeObject.IsVoor == null)
            {
                source.WozWaardeObject.IsVoor = new WozObject();
            }
            source.WozWaardeObject.IsVoor.WozObjectNummer = request.WozWaardeObject.IsVoor.WozObjectNummer;

            UpdateAanduidingWozObject(request, source);
        }
        else // request.WozWaardeObject.IsVoor = null
        {
            // Current DB value is filled so remove
            if (source.WozWaardeObject.IsVoor != null)
            {
                source.WozWaardeObject.IsVoor = null;
            }
        }
    }

    private static void UpdateAanduidingWozObject(ZaakObject request, ZaakObject source)
    {
        // Update/remove/add Child-child object
        if (request.WozWaardeObject.IsVoor.AanduidingWozObject != null)
        {
            if (source.WozWaardeObject.IsVoor.AanduidingWozObject == null)
            {
                source.WozWaardeObject.IsVoor.AanduidingWozObject = new AanduidingWozObject();
            }
            source.WozWaardeObject.IsVoor.AanduidingWozObject.AoaHuisletter = request.WozWaardeObject.IsVoor.AanduidingWozObject.AoaHuisletter;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.AoaHuisnummer = request.WozWaardeObject.IsVoor.AanduidingWozObject.AoaHuisnummer;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.AoaHuisnummerToevoeging = request
                .WozWaardeObject
                .IsVoor
                .AanduidingWozObject
                .AoaHuisnummerToevoeging;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.AoaIdentificatie = request.WozWaardeObject.IsVoor.AanduidingWozObject.AoaIdentificatie;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.AoaPostcode = request.WozWaardeObject.IsVoor.AanduidingWozObject.AoaPostcode;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.GorOpenbareRuimteNaam = request
                .WozWaardeObject
                .IsVoor
                .AanduidingWozObject
                .GorOpenbareRuimteNaam;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.LocatieOmschrijving = request
                .WozWaardeObject
                .IsVoor
                .AanduidingWozObject
                .LocatieOmschrijving;
            source.WozWaardeObject.IsVoor.AanduidingWozObject.WplWoonplaatsNaam = request
                .WozWaardeObject
                .IsVoor
                .AanduidingWozObject
                .WplWoonplaatsNaam;
        }
        else // request.WozWaardeObject.IsVoor.AanduidingWozObject = null
        {
            // Current DB value is filled so remove
            if (source.WozWaardeObject.IsVoor.AanduidingWozObject != null)
            {
                source.WozWaardeObject.IsVoor.AanduidingWozObject = null;
            }
        }
    }
}
