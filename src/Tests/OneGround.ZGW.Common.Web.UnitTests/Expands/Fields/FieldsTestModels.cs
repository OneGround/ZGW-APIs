using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

/// <summary>
/// Gedeelde response-DTO's voor de Fields-tests. De projectie/validatie leunen op
/// <see cref="JsonPropertyNameAttribute"/>: alleen properties met dit attribuut tellen mee,
/// en het attribuut <c>_expand</c> is de container voor geneste sub-entiteiten.
/// </summary>
public sealed class ChildDto
{
    [JsonPropertyName("naam")]
    public string? Naam { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class GrandChildDto
{
    [JsonPropertyName("waarde")]
    public string? Waarde { get; set; }
}

public sealed class ParentDto
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("identificatie")]
    public string? Identificatie { get; set; }

    // Géén JsonPropertyName → mag nooit geprojecteerd worden.
    public string? Intern { get; set; }

    [JsonPropertyName("_expand")]
    public Dictionary<string, object?>? Expand { get; set; }
}

// ── Inline geneste objecten (gepunte field-syntax: naam.veld) ──────────────────────
// Aparte DTO-graaf zodat de bestaande ParentDto-tests (vaste veldtelling) ongemoeid blijven.

public sealed class DeepDto
{
    [JsonPropertyName("x")]
    public string? X { get; set; }
}

public sealed class InnerDto
{
    [JsonPropertyName("a")]
    public string? A { get; set; }

    [JsonPropertyName("b")]
    public string? B { get; set; }

    [JsonPropertyName("diep")]
    public DeepDto? Diep { get; set; } // genest-in-genest (via reflectie)
}

// Polymorfe varianten voor een object-getypeerd veld (zoals betrokkeneIdentificatie).
public sealed class VariantOneDto
{
    [JsonPropertyName("een")]
    public string? Een { get; set; }

    [JsonPropertyName("adres")]
    public DeepDto? Adres { get; set; }
}

public sealed class VariantTwoDto
{
    [JsonPropertyName("twee")]
    public string? Twee { get; set; }
}

public sealed class HostDto
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("verlenging")]
    public InnerDto? Verlenging { get; set; } // concreet → reflectie

    [JsonPropertyName("kenmerken")]
    public List<InnerDto> Kenmerken { get; set; } = new(); // collectie → reflectie

    [JsonPropertyName("poly")]
    public object? Poly { get; set; } // polymorf → expliciet

    [JsonPropertyName("_expand")]
    public Dictionary<string, object?>? Expand { get; set; }
}
