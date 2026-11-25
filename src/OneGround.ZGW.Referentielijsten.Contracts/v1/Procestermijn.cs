using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneGround.ZGW.Referentielijsten.Contracts.v1;

// The converter is on the type (not only on the property) so that Swagger describes the enum with its string values.
[JsonConverter(typeof(StringEnumConverter))]
public enum Procestermijn
{
    /// <summary>No procestermijn: the standard allows an empty string next to the named values.</summary>
    [EnumMember(Value = "")]
    leeg,
    nihil,
    bestaansduur_procesobject,
    ingeschatte_bestaansduur_procesobject,
    vast_te_leggen_datum,
    samengevoegd_met_bewaartermijn,
    na_afhandeling,
    ander_kenmerk_procesobject,
    na_einde_procesobject,
}
