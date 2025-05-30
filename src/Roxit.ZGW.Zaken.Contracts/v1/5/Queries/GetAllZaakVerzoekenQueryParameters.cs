using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetAllZaakVerzoekenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar het VERZOEK (in de Klantinteractie API).
    /// </summary>
    [FromQuery(Name = "verzoek")]
    public string Verzoek { get; set; }
}
