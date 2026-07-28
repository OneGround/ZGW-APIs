using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1;

public abstract class ApplicatieDto
{
    [JsonProperty("clientIds", Order = 2)]
    public string[] ClientIds { get; set; }

    [JsonProperty("label", Order = 3)]
    public string Label { get; set; }

    [JsonProperty("alleenIsGereedVoorPublicatie", Order = 4, NullValueHandling = NullValueHandling.Ignore)] // This property is nullable to support backward compatibility with older versions of the API that do not include this field.
    public bool? AlleenIsGereedVoorPublicatie { get; set; }

    [JsonProperty("heeftAlleAutorisaties", Order = 5)]
    public bool HeeftAlleAutorisaties { get; set; }
}
