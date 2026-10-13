using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    private JToken _fields;

    // Note: an explicit JSON null ("fields": null) is deserialized into a JValue of type Null, not into null. It means the same as leaving
    // "fields" out, so it is normalized here -- otherwise it would be rejected as "not an array" or as a combination with "expand".
    [JsonProperty("fields")]
    public JToken Fields
    {
        get => _fields;
        set => _fields = value?.Type == JTokenType.Null ? null : value;
    }
}
