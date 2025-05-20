using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Queries;

public class GetAllVerzendingenQueryParameters : QueryParameters, IExpandQueryParameter
{
    /// <summary>
    /// Omschrijving van de aard van de relatie van de BETROKKENE tot het INFORMATIEOBJECT.
    /// </summary>
    [FromQuery(Name = "aardRelatie")] // Note: Enum: "afzender" "geadresseerde"
    public string AardRelatie { get; set; }

    /// <summary>
    /// URL-referentie naar het informatieobject dat is ontvangen of verzonden.
    /// </summary>
    [FromQuery(Name = "informatieobject")]
    public string InformatieObject { get; set; }

    /// <summary>
    /// URL-referentie naar de betrokkene waarvan het informatieobject is ontvangen of waaraan dit is verzonden.
    /// </summary>
    [FromQuery(Name = "betrokkene")]
    public string Betrokkene { get; set; }

    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
