using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

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

    [JsonProperty("zaaktypen", Order = 10)]
    public IEnumerable<string> ZaakTypen { get; set; }

    [JsonProperty("besluittypen", Order = 11)]
    public IEnumerable<string> BesluitTypen { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 12)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }
}
