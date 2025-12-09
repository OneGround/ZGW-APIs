using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1.Responses;

public class EnkelvoudigInformatieObjectResponseDto : EnkelvoudigInformatieObjectDto
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "versie")]
    public int Versie { get; set; }

    [JsonProperty(PropertyName = "beginRegistratie")]
    public string BeginRegistratie { get; set; }

    [JsonProperty(PropertyName = "bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [JsonProperty(PropertyName = "locked")]
    public bool Locked { get; set; }
}
