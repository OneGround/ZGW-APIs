using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

/// <summary>
/// Gedeelde response-DTO's voor de Fields-tests. De projectie/validatie leunen op
/// <see cref="JsonPropertyAttribute"/>: alleen properties met dit attribuut tellen mee,
/// en het attribuut <c>_expand</c> is de container voor geneste sub-entiteiten.
/// </summary>
public sealed class ChildDto
{
    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }
}

public sealed class GrandChildDto
{
    [JsonProperty("waarde")]
    public string Waarde { get; set; }
}

public sealed class ParentDto
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    // Géén JsonPropertyName → mag nooit geprojecteerd worden.
    public string Intern { get; set; }

    [JsonProperty("_expand")]
    public Dictionary<string, object> Expand { get; set; }
}

// ── Inline geneste objecten (gepunte field-syntax: naam.veld) ──────────────────────
// Aparte DTO-graaf zodat de bestaande ParentDto-tests (vaste veldtelling) ongemoeid blijven.

public sealed class DeepDto
{
    [JsonProperty("x")]
    public string X { get; set; }
}

public sealed class InnerDto
{
    [JsonProperty("a")]
    public string A { get; set; }

    [JsonProperty("b")]
    public string B { get; set; }

    [JsonProperty("diep")]
    public DeepDto Diep { get; set; } // genest-in-genest (via reflectie)
}

// Polymorfe varianten voor een object-getypeerd veld (zoals betrokkeneIdentificatie).
public sealed class VariantOneDto
{
    [JsonProperty("een")]
    public string Een { get; set; }

    [JsonProperty("adres")]
    public DeepDto Adres { get; set; }
}

public sealed class VariantTwoDto
{
    [JsonProperty("twee")]
    public string Twee { get; set; }
}

public sealed class HostDto
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("verlenging")]
    public InnerDto Verlenging { get; set; } // concreet → reflectie

    [JsonProperty("kenmerken")]
    public List<InnerDto> Kenmerken { get; set; } = new(); // collectie → reflectie

    [JsonProperty("poly")]
    public object Poly { get; set; } // polymorf → expliciet

    [JsonProperty("_expand")]
    public Dictionary<string, object> Expand { get; set; }
}
