using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1._1
{
    public abstract class ApplicatieDto
    {
        [JsonProperty("clientIds", Order = 2)]
        public string[] ClientIds { get; set; }

        [JsonProperty("label", Order = 3)]
        public string Label { get; set; }

        [JsonProperty(Order = 4)]
        public bool AlleenIsGereedVoorPublicatie { get; set; }

        [JsonProperty("heeftAlleAutorisaties", Order = 5)]
        public bool HeeftAlleAutorisaties { get; set; }
    }
}
