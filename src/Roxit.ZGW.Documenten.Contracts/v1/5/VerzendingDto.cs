using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5;

public class VerzendingDto
{
    [JsonProperty("betrokkene")]
    public string Betrokkene { get; set; }

    [JsonProperty("informatieobject")]
    public string InformatieObject { get; set; }

    [JsonProperty("aardRelatie")]
    public string AardRelatie { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("ontvangstdatum")]
    public string OntvangstDatum { get; set; }

    [JsonProperty("verzenddatum")]
    public string Verzenddatum { get; set; }

    [JsonProperty("contactPersoon")]
    public string Contactpersoon { get; set; }

    [JsonProperty("contactpersoonnaam")]
    public string ContactpersoonNaam { get; set; }

    [JsonProperty("binnenlandsCorrespondentieadres")]
    public BinnenlandsCorrespondentieAdresDto BinnenlandsCorrespondentieAdres { get; set; }

    [JsonProperty("buitenlandsCorrespondentieadres")]
    public BuitenlandsCorrespondentieAdresDto BuitenlandsCorrespondentieAdres { get; set; }

    [JsonProperty("correspondentiePostadres")]
    public CorrespondentiePostAdresDto CorrespondentiePostadres { get; set; }

    [JsonProperty("faxnummer")]
    public string Faxnummer { get; set; }

    [JsonProperty("emailadres")]
    public string EmailAdres { get; set; }

    [JsonProperty("mijnOverheid")]
    public bool MijnOverheid { get; set; }

    [JsonProperty("telefoonnummer")]
    public string Telefoonnummer { get; set; }
}
