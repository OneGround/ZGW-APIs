using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Notificaties.Contracts.v1;

public class NotificatieDto
{
    [JsonProperty(PropertyName = "kanaal")]
    public string Kanaal { get; set; }

    [JsonProperty(PropertyName = "hoofdObject")]
    public string HoofdObject { get; set; }

    [JsonProperty(PropertyName = "resource")]
    public string Resource { get; set; }

    [JsonProperty(PropertyName = "resourceUrl")]
    public string ResourceUrl { get; set; }

    [JsonProperty(PropertyName = "actie")]
    public string Actie { get; set; }

    [JsonProperty(PropertyName = "aanmaakdatum")]
    public string Aanmaakdatum { get; set; }

    [JsonProperty(PropertyName = "kenmerken")]
    public IDictionary<string, string> Kenmerken { get; set; }
}
