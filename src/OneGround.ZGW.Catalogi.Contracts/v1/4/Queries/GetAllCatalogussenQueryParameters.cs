using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;

public class GetAllCatalogussenQueryParameters : QueryParameters, IExpandParameter
{
    /// <summary>
    /// Een afkorting waarmee wordt aangegeven voor welk domein in een CATALOGUS ZAAKTYPEn zijn uitgewerkt.
    /// </summary>
    [FromQuery(Name = "domein")]
    public string Domein { get; set; }

    [FromQuery(Name = "domein__in")]
    public string Domein__in { get; set; }

    /// <summary>
    /// Een afkorting waarmee wordt aangegeven voor welk domein in een CATALOGUS ZAAKTYPEn zijn uitgewerkt.
    /// </summary>
    [FromQuery(Name = "rsin")]
    public string Rsin { get; set; }

    [FromQuery(Name = "rsin__in")]
    public string Rsin__in { get; set; }

    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
