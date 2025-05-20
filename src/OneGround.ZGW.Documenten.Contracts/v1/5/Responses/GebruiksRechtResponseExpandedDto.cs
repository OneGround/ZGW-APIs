using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Responses;

public class GebruiksRechtResponseExpandedDto : v1.Responses.GebruiksRechtResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
