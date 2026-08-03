using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;

public class ApplicatieRequestDto : ApplicatieDto
{
    [JsonProperty("autorisaties")]
    public List<v1.Requests.AutorisatieRequestDto> Autorisaties { get; set; } = [];
}
