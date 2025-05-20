using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllBesluitTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de CATALOGUS waartoe dit BESLUITTYPE behoort.
    /// </summary>
    [FromQuery(Name = "catalogus")]
    public string Catalogus { get; set; }

    /// <summary>
    /// ZAAKTYPE met ZAAKen die relevant kunnen zijn voor dit BESLUITTYPE.
    /// </summary>
    [FromQuery(Name = "zaaktypen")]
    public string ZaakType { get; set; }

    /// <summary>
    /// Het INFORMATIEOBJECTTYPE van informatieobjecten waarin besluiten van dit BESLUITTYPE worden vastgelegd.
    /// </summary>
    [FromQuery(Name = "informatieobjecttypen")]
    public string InformatieObjectType { get; set; }

    /// <summary>
    /// filter objects depending on their concept status:
    ///    alles: Toon objecten waarvan het attribuut concept true of false is.
    ///    concept: Toon objecten waarvan het attribuut concept true is.
    ///    definitief: Toon objecten waarvan het attribuut concept false is (standaard).
    /// </summary>
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// Omschrijving van de aard van BESLUITen van het BESLUITTYPE.
    /// </summary>
    [FromQuery(Name = "omschrijving")]
    public string Omschrijving { get; set; }

    /// <summary>
    /// Filter objecten op hun geldigheids datum.
    /// </summary>
    [FromQuery(Name = "datumGeldigheid")]
    public string DatumGeldigheid { get; set; }
}
