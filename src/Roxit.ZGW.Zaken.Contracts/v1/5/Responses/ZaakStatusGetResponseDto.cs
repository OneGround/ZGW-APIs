using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakStatusGetResponseDto : ZaakStatusCreateResponseDto
{
    [JsonProperty("zaakinformatieobjecten", Order = 11)]
    public IEnumerable<string> ZaakInformatieObjecten { get; set; }
}
