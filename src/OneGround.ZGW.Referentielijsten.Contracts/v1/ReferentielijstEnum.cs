using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OneGround.ZGW.Referentielijsten.Contracts.v1;

/// <summary>
/// Parses the string form of the reference-list enums (the <see cref="EnumMemberAttribute"/> value, or the member name) as used in the API and in the
/// static data. A null or empty string is the empty member, if the enum has one. Matching is case-sensitive.
/// </summary>
public static class ReferentielijstEnum
{
    public static bool TryParse<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        return EnumValues<TEnum>.ByApiValue.TryGetValue(value ?? string.Empty, out result);
    }

    /// <summary>Like <see cref="TryParse{TEnum}"/>, but throws a descriptive exception for an unknown value.</summary>
    public static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
    {
        if (TryParse<TEnum>(value, out var result))
        {
            return result;
        }

        throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}. Allowed values: {AllowedValues<TEnum>()}.");
    }

    /// <summary>The values as used in the API, comma separated; the empty value is shown as (empty).</summary>
    public static string AllowedValues<TEnum>()
        where TEnum : struct, Enum
    {
        return string.Join(", ", EnumValues<TEnum>.ByApiValue.Keys.Select(v => v.Length == 0 ? "(empty)" : v));
    }

    // The lookup per enum is built once: the parsing runs for every record of every list request.
    private static class EnumValues<TEnum>
        where TEnum : struct, Enum
    {
        public static readonly IReadOnlyDictionary<string, TEnum> ByApiValue = typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(f => f.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? f.Name, f => (TEnum)f.GetValue(null)!);
    }
}
