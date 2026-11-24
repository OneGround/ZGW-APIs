using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OneGround.ZGW.Referentielijsten.Contracts.v1;

/// <summary>
/// Parses the string form of the reference-list enums (the <see cref="EnumMemberAttribute"/> value, or the member name) as used in the API and in the
/// static data. A null or empty string is the empty member, if the enum has one.
/// </summary>
public static class ReferentielijstEnum
{
    public static bool TryParse<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        value ??= string.Empty;

        foreach (var member in Enum.GetValues<TEnum>())
        {
            if (GetApiValue(member) == value)
            {
                result = member;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>Like <see cref="TryParse{TEnum}"/>, but throws a descriptive exception for an unknown value.</summary>
    public static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
    {
        if (TryParse<TEnum>(value, out var result))
        {
            return result;
        }

        var allowed = string.Join(", ", Enum.GetValues<TEnum>().Select(m => string.IsNullOrEmpty(GetApiValue(m)) ? "(empty)" : GetApiValue(m)));
        throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}. Allowed values: {allowed}.");
    }

    private static string GetApiValue<TEnum>(TEnum member)
        where TEnum : struct, Enum
    {
        var name = member.ToString();
        var attribute = typeof(TEnum).GetField(name)?.GetCustomAttribute<EnumMemberAttribute>();
        return attribute?.Value ?? name;
    }
}
