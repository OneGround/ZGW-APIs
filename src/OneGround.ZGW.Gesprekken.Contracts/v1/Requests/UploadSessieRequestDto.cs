using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

public class UploadSessieRequestDto
{
    [JsonProperty("bestandsnaam", Order = 1)]
    public string Bestandsnaam { get; set; }

    [JsonProperty("bestandsomvang", Order = 2)]
    public long Bestandsomvang { get; set; }

    [JsonProperty("contentType", Order = 3)]
    public string ContentType { get; set; }

    [JsonProperty("titel", Order = 4)]
    public string Titel { get; set; }
}
