using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

/// <summary>
/// v1.4 variant of the v1.3 response: the same resource, extended with the optional <c>_expand</c> (see the <c>expand</c> query parameter).
/// </summary>
public class ZaakTypeResponseDto : _3.Responses.ZaakTypeResponseDto, IExpandable
{
    // Order: JSON property order (Newtonsoft sorts by Order, and for equal values puts the members of the derived class before those of its base). The
    // lists below take the Order of the property that FOLLOWS their partner in the v1.3 DTOs, so they are serialized directly after it (e.g. zaaktypen,
    // zaaktypeIdentificaties). Pinned by DomainToResponseV14AdditionsTests.V1_4_additions_follow_the_property_they_belong_to.
    /// <summary>
    /// Hides the v1.3 member of the same name to give its elements the v1.4 type (with <c>zaaktypeIdentificatie</c>). The mapping fills this one; the
    /// hidden v1.3 member is not serialized.
    /// </summary>
    [JsonProperty("gerelateerdeZaaktypen", Order = 44)]
    public new IEnumerable<GerelateerdeZaaktypeResponseDto> GerelateerdeZaakTypen { get; set; } = [];

    /// <summary>Omschrijvingen van de BESLUITTYPEn die bij dit ZAAKTYPE horen.</summary>
    [JsonProperty("besluittypeOmschrijving", Order = 42)]
    public IEnumerable<string> BesluitTypeOmschrijving { get; set; } = [];

    /// <summary>Omschrijvingen van de INFORMATIEOBJECTTYPEn die mogelijk zijn binnen dit ZAAKTYPE.</summary>
    [JsonProperty("informatieobjecttypeOmschrijving", Order = 37)]
    public IEnumerable<string> InformatieObjectTypeOmschrijving { get; set; } = [];

    /// <summary>Identificaties van de deelzaaktypen van dit ZAAKTYPE.</summary>
    [JsonProperty("deelzaaktypeIdentificaties", Order = 43)]
    public IEnumerable<string> DeelZaakTypeIdentificaties { get; set; } = [];

    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
