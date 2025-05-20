namespace OneGround.ZGW.Common.DataModel;

public enum Afleidingswijze
{
    /// <summary>
    /// De termijn start op de datum waarop de zaak is afgehandeld (ZAAK.Einddatum in het RGBZ).
    /// </summary>
    afgehandeld,

    /// <summary>
    /// De termijn start op de datum die is vastgelegd in een ander datumveld dan de datumvelden
    /// waarop de overige waarden (van deze attribuutsoort) betrekking hebben.
    /// Objecttype, Registratie en Datumkenmerk zijn niet leeg.
    /// </summary>
    ander_datumkenmerk,

    /// <summary>
    /// De termijn start op de datum die vermeld is in een zaaktype-specifieke eigenschap (zijnde een datumveld).
    /// ResultaatType.ZaakType heeft een Eigenschap; Objecttype, en Datumkenmerk zijn niet leeg.
    /// </summary>
    eigenschap,

    /// <summary>
    /// De termijn start op de datum waarop de gerelateerde zaak is afgehandeld
    /// (ZAAK.Einddatum of ZAAK.Gerelateerde_zaak.Einddatum in het RGBZ).
    /// ResultaatType.ZaakType heeft gerelateerd ZaakType
    /// </summary>
    gerelateerde_zaak,

    /// <summary>
    /// De termijn start op de datum waarop de gerelateerde zaak is afgehandeld, waarvan de zaak een deelzaak is
    /// (ZAAK.Einddatum van de hoofdzaak in het RGBZ). ResultaatType.ZaakType is deelzaaktype van <see cref="ZaakType"/>.
    /// </summary>
    hoofdzaak,

    /// <summary>
    /// De termijn start op de datum waarop het besluit van kracht wordt (BESLUIT.Ingangsdatum in het RGBZ).
    /// ResultaatType.ZaakType heeft relevant BesluitType.
    /// </summary>
    ingangsdatum_besluit,

    /// <summary>
    /// De termijn start een vast aantal jaren na de datum waarop de zaak is afgehandeld (ZAAK.Einddatum in het RGBZ).
    /// </summary>
    termijn,

    /// <summary>
    /// De termijn start op de dag na de datum waarop het besluit vervalt (BESLUIT.Vervaldatum in het RGBZ).
    /// ResultaatType.ZaakType heeft relevant BesluitType.
    /// </summary>
    vervaldatum_besluit,

    /// <summary>
    /// De termijn start op de einddatum geldigheid van het zaakobject waarop de zaak betrekking heeft (bijvoorbeeld de overlijdendatum van een Persoon). M.b.v. de attribuutsoort Objecttype wordt vastgelegd om welke zaakobjecttype het gaat; m.b.v. de attribuutsoort Datumkenmerk wordt vastgelegd welke datum-attribuutsoort van het zaakobjecttype het betreft.
    /// </summary>
    zaakobject,
}
