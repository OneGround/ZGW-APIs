using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._1.Responses;

public class EnkelvoudigInformatieObjectResponseDto : EnkelvoudigInformatieObjectDto
{
    [JsonProperty(PropertyName = "url", Order = -1)]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "versie", Order = 70)]
    public int Versie { get; set; }

    [JsonProperty(PropertyName = "beginRegistratie", Order = 71)]
    public DateTime BeginRegistratie { get; set; }

    [JsonProperty(PropertyName = "locked", Order = 72)]
    public bool Locked { get; set; }

    [JsonProperty("bestandsdelen", Order = 73)]
    public List<BestandsDeelResponseDto> BestandsDelen { get; set; } = [];
}
