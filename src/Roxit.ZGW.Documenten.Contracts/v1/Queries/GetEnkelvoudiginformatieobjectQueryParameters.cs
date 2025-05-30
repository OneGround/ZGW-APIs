using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Documenten.Contracts.v1.Queries;

public class GetEnkelvoudigInformatieObjectQueryParameters : QueryParameters
{
    /// <summary>
    /// Het (automatische) versienummer van het INFORMATIEOBJECT.
    /// </summary>
    [FromQuery(Name = "versie")]
    public string Versie { get; set; }

    /// <summary>
    /// Een datumtijd in ISO8601 formaat.
    /// De versie van het INFORMATIEOBJECT die qua begin_registratie het kortst hiervoor zit wordt opgehaald.
    /// </summary>
    [FromQuery(Name = "registratieOp")]
    public string RegistratieOp { get; set; }
}
