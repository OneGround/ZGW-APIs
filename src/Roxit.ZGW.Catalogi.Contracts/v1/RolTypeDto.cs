using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public abstract class RolTypeDto
{
    [JsonProperty("zaaktype", Order = 2)]
    public string ZaakType { get; set; }

    [JsonProperty("omschrijving", Order = 3)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 4)]
    public string OmschrijvingGeneriek { get; set; }
}
