using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._7.Requests;

public class EnkelvoudigInformatieObjectSearchRequestDto : IDocumentenCommonSearchableFields, IExpandParameter
{
    [JsonProperty("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("trefwoorden")]
    public string Trefwoorden { get; set; }

    [JsonProperty("objectinformatieobjecten_object")]
    public string ObjectInformatieObjecten_Object { get; set; }

    [JsonProperty("objectinformatieobjecten_objectType")]
    public string ObjectInformatieObjecten_ObjectType { get; set; }

    [JsonProperty("uuid_In")]
    public string[] Uuid_In { get; set; }

    [JsonProperty("expand")]
    public string Expand { get; set; }
}
