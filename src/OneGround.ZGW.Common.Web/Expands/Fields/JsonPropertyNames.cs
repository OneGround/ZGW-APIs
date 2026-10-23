using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

/// <summary>
/// Resolves the effective JSON property name for a <see cref="JsonPropertyAttribute"/>-carrying
/// member, mirroring what the real ASP.NET Core Newtonsoft serializer actually puts on the wire.
/// <para>
/// A <c>[JsonProperty]</c> attribute does not require an explicit name -- e.g. the real
/// <c>BetrokkeneIdentificatie</c> property on the Rol subtype response DTOs only carries
/// <c>[JsonProperty(Order = 1000)]</c>. When <see cref="JsonPropertyAttribute.PropertyName"/> is left
/// unset, Newtonsoft falls back to the contract resolver's naming strategy applied to the member's
/// own name; every ZGW API host registers Newtonsoft via <c>AddNewtonsoftJson()</c> without
/// overriding <c>ContractResolver</c>, so that's ASP.NET Core's own default,
/// <see cref="CamelCasePropertyNamesContractResolver"/>. Treating an unset <c>PropertyName</c> as
/// "no name" (as opposed to replicating that fallback) either crashes (a null dictionary key) or
/// silently excludes the field, depending on the call site -- this is the one place that decision is
/// made, so every Fields/expand call site agrees with the real serializer.
/// </para>
/// </summary>
internal static class JsonPropertyNames
{
    private static readonly NamingStrategy CamelCase = new CamelCaseNamingStrategy();

    public static string Resolve(JsonPropertyAttribute attribute, PropertyInfo property) =>
        string.IsNullOrEmpty(attribute.PropertyName) ? CamelCase.GetPropertyName(property.Name, hasSpecifiedName: false) : attribute.PropertyName;
}
