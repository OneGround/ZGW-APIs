using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1.Responses;

public class AutorisatieResponseDto : AutorisatieDto
{
    [JsonProperty("componentWeergave", Order = 2)]
    public string ComponentWeergave { get; set; }
}
