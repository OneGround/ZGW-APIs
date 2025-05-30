using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1.Queries;

public class GetAllZaakInformatieObjectenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar het INFORMATIEOBJECT (in de Documenten API), waar ook de relatieinformatie opgevraagd kan worden.
    /// </summary>
    [FromQuery(Name = "informatieobject")]
    public string InformatieObject { get; set; }
}
