using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses;

public class ApplicatieResponseDto : ApplicatieDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("autorisaties", Order = 6)]
    public List<v1.Responses.AutorisatieResponseDto> Autorisaties { get; set; }
}
