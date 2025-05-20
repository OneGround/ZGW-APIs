using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetAllZaakStatussenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar het STATUSTYPE (in de Catalogi API).
    /// </summary>
    [FromQuery(Name = "statustype")]
    public string StatusType { get; set; }

    /// <summary>
    /// Het gegeven is afleidbaar uit de historie van de attribuutsoort Datum status gezet van van alle statussen bij de desbetreffende zaak.
    /// </summary>
    [FromQuery(Name = "indicatieLaatstGezetteStatus")]
    public string IndicatieLaatstGezetteStatus { get; set; }
}
