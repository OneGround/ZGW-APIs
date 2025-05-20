using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllResultaatTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het ZAAKTYPE van ZAAKen waarin resultaten van dit RESULTAATTYPE bereikt kunnen worden.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// Filter objects depending on their concept status:
    ///  -alles:      Toon objecten waarvan het attribuut concept true of false is.
    ///  -concept:    Toon objecten waarvan het attribuut concept true is.
    ///  -definitief: Toon objecten waarvan het attribuut concept false is (standaard).
    /// </summary>
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// Filter on ZaaktypeIdentificatie.
    /// </summary>
    [FromQuery(Name = "zaaktypeIdentificatie")]
    public string ZaaktypeIdentificatie { get; set; }

    /// <summary>
    /// Filter objecten op hun geldigheids datum.
    /// </summary>
    [FromQuery(Name = "datumGeldigheid")]
    public string DatumGeldigheid { get; set; }
}
