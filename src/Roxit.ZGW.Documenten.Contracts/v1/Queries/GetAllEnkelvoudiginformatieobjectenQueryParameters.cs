using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Documenten.Contracts.v1.Queries;

public class GetAllEnkelvoudigInformatieObjectenQueryParameters : QueryParameters
{
    /// <summary>
    /// Het RSIN van de Niet-natuurlijk persoon zijnde de organisatie die het informatieobject heeft gecreëerd of heeft ontvangen en als eerste in een samenwerkingsketen heeft vastgelegd.
    /// </summary>
    [FromQuery(Name = "bronorganisatie")]
    public string Bronorganisatie { get; set; }

    /// <summary>
    /// Een binnen een gegeven context ondubbelzinnige referentie naar het INFORMATIEOBJECT.
    /// </summary>
    [FromQuery(Name = "identificatie")]
    public string Identificatie { get; set; }
}
