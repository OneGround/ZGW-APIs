using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

public class ReserveZaakIdentificatieResponseDto
{
    /// <summary>
    /// De unieke identificatie van de ZAAK binnen de organisatie die verantwoordelijk is voor de behandeling van de ZAAK.
    /// </summary>
    [JsonProperty("zaaknummer")]
    public string Zaaknummer { get; set; }
}
