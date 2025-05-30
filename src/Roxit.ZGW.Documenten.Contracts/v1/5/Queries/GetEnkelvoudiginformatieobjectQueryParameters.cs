using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Queries;

public interface IGetEnkelvoudigInformatieObjectQueryParameters
{
    /// <summary>
    /// Het (automatische) versienummer van het INFORMATIEOBJECT.
    /// </summary>
    string Versie { get; set; }

    /// <summary>
    /// Een datumtijd in ISO8601 formaat.
    /// De versie van het INFORMATIEOBJECT die qua begin_registratie het kortst hiervoor zit wordt opgehaald.
    /// </summary>
    string RegistratieOp { get; set; }
}

public class GetEnkelvoudigInformatieObjectQueryParameters : QueryParameters, IGetEnkelvoudigInformatieObjectQueryParameters, IExpandQueryParameter
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

    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}

public class DownloadEnkelvoudigInformatieObjectQueryParameters : QueryParameters, IGetEnkelvoudigInformatieObjectQueryParameters
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
