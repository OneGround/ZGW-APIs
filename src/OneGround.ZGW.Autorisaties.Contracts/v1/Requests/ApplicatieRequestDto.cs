using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1.Requests;

public class ApplicatieRequestDto : ApplicatieDto
{
    [JsonProperty("autorisaties")]
    public List<AutorisatieRequestDto> Autorisaties { get; set; } = [];
}
