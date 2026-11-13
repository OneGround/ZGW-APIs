using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

/// <summary>
/// v1.4 variant of the v1.3 response: the same resource, extended with the optional <c>_expand</c> (see the <c>expand</c> query parameter).
/// </summary>
public class ZaakObjectTypeResponseDto : _3.Responses.ZaakObjectTypeResponseDto, IExpandable
{
    // Order: JSON property order (Newtonsoft sorts by Order, and for equal values puts the members of the derived class before those of its base). The
    // lists below take the Order of the property that FOLLOWS their partner in the v1.3 DTOs, so they are serialized directly after it (e.g. zaaktypen,
    // zaaktypeIdentificaties). Pinned by DomainToResponseV14AdditionsTests.V1_4_additions_follow_the_property_they_belong_to.
    /// <summary>Omschrijvingen van de RESULTAATTYPEn van dit ZAAKOBJECTTYPE. Altijd leeg zolang de relatie niet kan worden vastgelegd (zie resultaattypen).</summary>
    [JsonProperty("resultaattypeOmschrijving", Order = 17)]
    public IEnumerable<string> ResultaatTypeOmschrijving { get; set; } = [];

    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
