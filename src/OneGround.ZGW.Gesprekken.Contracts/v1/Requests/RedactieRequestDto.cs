using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

/// <summary>
/// Redigeren van een gespreksbijdrage (beheer). De reden gaat naar het log van de registratie, niet in het event.
/// </summary>
public class RedactieRequestDto
{
    [JsonProperty("reden", Order = 1)]
    public string Reden { get; set; }
}
