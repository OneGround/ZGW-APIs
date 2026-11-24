using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneGround.ZGW.Referentielijsten.Contracts.v1;

// The converter is on the type (not only on the property) so that Swagger describes the enum with its string values.
[JsonConverter(typeof(StringEnumConverter))]
public enum Waardering
{
    /// <summary>No waardering: the standard allows an empty string next to the named values.</summary>
    [EnumMember(Value = "")]
    leeg,
    blijvend_bewaren,
    vernietigen,
}
