using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class CatalogusResponseDto : CatalogusDto
{
    public CatalogusResponseDto()
    {
        ZaakTypen = [];
        BesluitTypen = [];
        InformatieObjectTypen = [];
    }

    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaaktypen", Order = 7)]
    public IEnumerable<string> ZaakTypen { get; set; }

    [JsonProperty("besluittypen", Order = 8)]
    public IEnumerable<string> BesluitTypen { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 9)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }
}
