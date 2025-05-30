using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Notificaties.Contracts.v1;

public class AbonnementDto
{
    [JsonProperty(PropertyName = "callbackUrl")]
    public string CallbackUrl { get; set; }

    [JsonProperty(PropertyName = "auth")]
    public string Auth { get; set; }

    [JsonProperty(PropertyName = "kanalen")]
    public IList<AbonnementKanaalDto> Kanalen { get; set; }
}
