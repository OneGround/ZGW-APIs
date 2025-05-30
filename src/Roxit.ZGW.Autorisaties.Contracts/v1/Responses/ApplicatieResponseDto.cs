using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Autorisaties.Contracts.v1.Responses;

public class ApplicatieResponseDto : ApplicatieDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("autorisaties", Order = 5)]
    public List<AutorisatieResponseDto> Autorisaties { get; set; }
}
