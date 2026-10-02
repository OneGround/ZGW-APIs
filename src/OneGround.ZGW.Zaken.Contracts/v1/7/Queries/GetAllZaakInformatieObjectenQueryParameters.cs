using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: v1._7-specific because it adds "expand" -- everything else is identical to
// v1.Queries.GetAllZaakInformatieObjectenQueryParameters. See
// Controllers/v1/7/ZaakInformatieObjectenController.cs.
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

    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
