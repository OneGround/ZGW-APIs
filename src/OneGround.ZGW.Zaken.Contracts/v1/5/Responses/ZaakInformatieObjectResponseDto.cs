using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakInformatieObjectResponseDto : ZaakInformatieObjectDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }

    [JsonProperty("aardRelatieWeergave", Order = 9)]
    public string AardRelatieWeergave { get; set; }

    [JsonProperty("registratiedatum", Order = 10)]
    public string RegistratieDatum { get; set; }
}
