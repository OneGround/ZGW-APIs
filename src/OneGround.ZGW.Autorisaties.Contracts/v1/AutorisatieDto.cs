using Newtonsoft.Json;

namespace OneGround.ZGW.Autorisaties.Contracts.v1;

public class AutorisatieDto
{
    [JsonProperty("component", Order = 1)]
    public string Component { get; set; }

    [JsonProperty("scopes", Order = 3)]
    public string[] Scopes { get; set; }

    [JsonProperty("zaaktype", Order = 4)]
    public string ZaakType { get; set; }

    [JsonProperty("informatieobjecttype", Order = 4)]
    public string InformatieObjectType { get; set; }

    [JsonProperty("besluittype", Order = 4)]
    public string BesluitType { get; set; }

    [JsonProperty("maxVertrouwelijkheidaanduiding", Order = 5)]
    public string MaxVertrouwelijkheidaanduiding { get; set; }

    public bool ShouldSerializeBesluitType()
    {
        return Component == "brc";
    }

    public bool ShouldSerializeZaakType()
    {
        return Component == "zrc";
    }

    public bool ShouldSerializeInformatieObjectType()
    {
        return Component == "drc";
    }

    public bool ShouldSerializeMaxVertrouwelijkheidaanduiding()
    {
        return Component == "zrc" || Component == "drc";
    }
}
