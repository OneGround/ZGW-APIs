using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public class ContactpersoonRolDto
{
    [JsonProperty("emailadres", Order = 1)]
    public string EmailAdres { get; set; }

    [JsonProperty("functie", Order = 2)]
    public string Functie { get; set; }

    [JsonProperty("telefoonnummer", Order = 3)]
    public string Telefoonnummer { get; set; }

    [JsonProperty("naam", Order = 4)]
    public string Naam { get; set; }
}
