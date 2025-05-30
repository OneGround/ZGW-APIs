using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Queries;

public class GetAllInformatieObjectTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de CATALOGUS waartoe dit INFORMATIEOBJECTTYPE behoort.
    /// </summary>
    [FromQuery(Name = "catalogus")]
    public string Catalogus { get; set; }

    /// <summary>
    /// filter objects depending on their concept status:
    ///    alles: Toon objecten waarvan het attribuut concept true of false is.
    ///    concept: Toon objecten waarvan het attribuut concept true is.
    ///    definitief: Toon objecten waarvan het attribuut concept false is (standaard).
    /// </summary>
    [FromQuery(Name = "status")]
    public string Status { get; set; }
}
