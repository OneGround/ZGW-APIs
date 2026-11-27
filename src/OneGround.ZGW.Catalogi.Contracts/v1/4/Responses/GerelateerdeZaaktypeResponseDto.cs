using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

/// <summary>
/// v1.4 variant of a related zaaktype in the response: the v1 element, extended with the read-only <c>zaaktypeIdentificatie</c>.
/// </summary>
public class GerelateerdeZaaktypeResponseDto : GerelateerdeZaaktypeDto
{
    /// <summary>De identificatie van het gerelateerde ZAAKTYPE.</summary>
    [JsonProperty("zaaktypeIdentificatie", Order = 1)]
    public string ZaaktypeIdentificatie { get; set; }
}
