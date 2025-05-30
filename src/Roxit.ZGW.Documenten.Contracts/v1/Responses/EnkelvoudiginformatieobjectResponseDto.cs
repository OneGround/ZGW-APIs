using System;
using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1.Responses;

public class EnkelvoudigInformatieObjectResponseDto : EnkelvoudigInformatieObjectDto
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "versie")]
    public int Versie { get; set; }

    [JsonProperty(PropertyName = "beginRegistratie")]
    public DateTime BeginRegistratie { get; set; }

    [JsonProperty(PropertyName = "bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [JsonProperty(PropertyName = "locked")]
    public bool Locked { get; set; }

    // FUND-1595: latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
    [JsonProperty(PropertyName = "latestEnkelvoudigInformatieObjectVersieId", Order = 9999)]
    public Guid? LatestEnkelvoudigInformatieObjectVersieId { get; set; }
}
