using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Responses;

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
    public List<_1.Responses.BestandsDeelResponseDto> BestandsDelen { get; set; } = [];

    // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
    [JsonProperty(PropertyName = "latestEnkelvoudigInformatieObjectVersieId", Order = 9999)]
    public Guid? LatestEnkelvoudigInformatieObjectVersieId { get; set; }
}
