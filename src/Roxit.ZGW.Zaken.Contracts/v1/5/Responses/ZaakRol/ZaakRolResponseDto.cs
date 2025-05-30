using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;

public class ZaakRolResponseDto : ZaakRolDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }

    [JsonProperty("omschrijving", Order = 7)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 8)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("registratiedatum", Order = 10)]
    public string Registratiedatum { get; set; }

    [JsonProperty("statussen", Order = 15)]
    public IEnumerable<string> Statussen { get; set; }
}
