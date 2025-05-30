using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllZaakObjectTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// Aanduiding waarmee wordt aangegeven of het ZAAKOBJECTTYPE een ander, niet in RSGB en RGBZ voorkomend, objecttype betreft.
    /// </summary>
    [FromQuery(Name = "anderObjecttype")]
    public string AnderObjectType { get; set; }

    /// <summary>
    /// URL-referentie naar de CATALOGUS waartoe dit ZAAKOBJECTTYPE behoort.
    /// </summary>
    [FromQuery(Name = "catalogus")]
    public string Catalogus { get; set; }

    /// <summary>
    /// De datum waarop het is ontstaan.
    /// </summary>
    [FromQuery(Name = "datumBeginGeldigheid")]
    public string DatumBeginGeldigheid { get; set; }

    /// <summary>
    /// De datum waarop het is opgeheven.
    /// </summary>
    [FromQuery(Name = "datumEindeGeldigheid")]
    public string DatumEindeGeldigheid { get; set; }

    /// <summary>
    /// filter objecten op hun geldigheids datum.
    /// </summary>
    [FromQuery(Name = "datumGeldigheid")]
    public string DatumGeldigheid { get; set; }

    /// <summary>
    /// URL-referentie naar de OBJECTTYPE waartoe dit ZAAKOBJECTTYPE behoort.
    /// </summary>
    [FromQuery(Name = "objecttype")]
    public string ObjectType { get; set; }

    /// <summary>
    /// Omschrijving van de betrekking van het Objecttype op zaken van het gerelateerde ZAAKTYPE.
    /// </summary>
    [FromQuery(Name = "relatieOmschrijving")]
    public string RelatieOmschrijving { get; set; }

    /// <summary>
    /// URL-referentie naar de ZAAKTYPE waartoe dit ZAAKOBJECTTYPE behoort.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// zaaktype_identificatie.
    /// </summary>
    [FromQuery(Name = "zaaktypeIdentificatie")]
    public string ZaaktypeIdentificatie { get; set; }
}
