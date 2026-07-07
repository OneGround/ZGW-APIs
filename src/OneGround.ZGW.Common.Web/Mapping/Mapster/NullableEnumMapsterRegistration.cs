using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapster;

namespace OneGround.ZGW.Common.Web.Mapping.Mapster;

/// <summary>
/// Mapster replacement for NullableEnumMapper: for every Nullable&lt;enum&gt; property found in the
/// scanned assemblies, an empty/null string maps to null and an unknown name maps to null.
/// </summary>
public static class NullableEnumMapsterRegistration
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for every distinct <c>Nullable&lt;enum&gt;</c> property type and
    /// registers a string-&gt;that-enum conversion rule for each one on <paramref name="config"/>.
    /// </summary>
    /// <remarks>
    /// Unlike the AutoMapper original, which matched any string-&gt;Nullable&lt;enum&gt; <c>TypePair</c>
    /// globally regardless of declaring assembly, this rule discovery is scoped to
    /// <paramref name="assemblies"/>: callers must pass every assembly that declares a nullable enum
    /// type they map into, or no rule will be registered for those types.
    /// </remarks>
    public static void RegisterNullableEnumRules(this TypeAdapterConfig config, IEnumerable<Assembly> assemblies)
    {
        var nullableEnumTypes = assemblies
            .SelectMany(GetLoadableTypes)
            .SelectMany(t => t.GetProperties())
            .Select(p => Nullable.GetUnderlyingType(p.PropertyType))
            .Where(u => u is { IsEnum: true })
            .Distinct()
            .ToList();

        var register =
            typeof(NullableEnumMapsterRegistration).GetMethod(nameof(RegisterForEnum), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{nameof(RegisterForEnum)} method not found via reflection.");

        foreach (var enumType in nullableEnumTypes)
        {
            register.MakeGenericMethod(enumType).Invoke(null, new object[] { config });
        }
    }

    internal static TEnum? ParseNullableEnum<TEnum>(string source)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        if (Enum.IsDefined(typeof(TEnum), source) && Enum.TryParse<TEnum>(source, out var result))
        {
            return result;
        }

        return null;
    }

    private static void RegisterForEnum<TEnum>(TypeAdapterConfig config)
        where TEnum : struct, Enum
    {
        config.NewConfig<string, TEnum?>().MapWith(src => ParseNullableEnum<TEnum>(src));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null);
        }
    }
}
