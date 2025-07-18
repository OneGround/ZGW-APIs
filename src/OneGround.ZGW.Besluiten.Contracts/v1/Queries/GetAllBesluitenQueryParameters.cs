using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Besluiten.Contracts.v1.Queries;

public class GetAllBesluitenQueryParameters : QueryParameters, IExpandParameter
{
    /// <summary>
    /// Identificatie van het besluit binnen de organisatie die het besluit heeft vastgesteld. Indien deze niet opgegeven is, dan wordt die gegenereerd.
    /// </summary>
    [FromQuery(Name = "identificatie")]
    public string Identificatie { get; set; }

    /// <summary>
    /// Het RSIN van de niet-natuurlijk persoon zijnde de organisatie die het besluit heeft vastgesteld.
    /// </summary>
    [FromQuery(Name = "verantwoordelijkeOrganisatie")]
    public string VerantwoordelijkeOrganisatie { get; set; }

    /// <summary>
    /// URL-referentie naar het BESLUITTYPE(in de Catalogi API).
    /// </summary>
    [FromQuery(Name = "besluittype")]
    public string BesluitType { get; set; }

    /// <summary>
    /// URL-referentie naar de ZAAK(in de Zaken API) waarvan dit besluit uitkomst is.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
