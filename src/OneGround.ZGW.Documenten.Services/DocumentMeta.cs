﻿using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Services;

public class DocumentMeta
{
    [JsonProperty("rsin")]
    public string Rsin { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("version")]
    public int Version { get; set; }
}
