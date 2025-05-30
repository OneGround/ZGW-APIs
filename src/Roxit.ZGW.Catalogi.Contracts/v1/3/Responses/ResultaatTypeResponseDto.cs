using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;

public class ResultaatTypeResponseDto : ResultaatTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 3)]
    public string ZaaktypeIdentificatie { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 6)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("catalogus", Order = 51)]
    public string Catalogus { get; set; }

    [JsonProperty("besluittypeOmschrijving", Order = 61)]
    public IEnumerable<string> BesluittypeOmschrijvingen { get; set; }

    [JsonProperty("informatieobjecttypeOmschrijving", Order = 63)]
    public IEnumerable<string> InformatieObjectTypeOmschrijvingen { get; set; }
}
