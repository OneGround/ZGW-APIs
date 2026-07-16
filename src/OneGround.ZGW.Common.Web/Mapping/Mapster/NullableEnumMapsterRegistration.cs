using System;
using System.Linq.Expressions;
using System.Reflection;
using Mapster;

namespace OneGround.ZGW.Common.Web.Mapping.Mapster;

/// <summary>
/// Mapster replacement for NullableEnumMapper: a single global rule matching any
/// string -&gt; Nullable&lt;enum&gt; conversion, regardless of which assembly declares the enum. An
/// empty/null string maps to null and an unrecognized name also maps to null (not an exception).
/// </summary>
/// <remarks>
/// This requires no per-service or per-assembly registration, unlike the assembly-scanning design
/// it replaces: <c>TypeAdapterConfig.When(...)</c> matches the source/destination type shape
/// directly (mirroring AutoMapper's original global TypePair-based <c>ObjectMapper.IsMatch</c>
/// behavior), and the converter factory is invoked lazily by Mapster at compile time with the
/// concrete enum type already known — so one rule covers every Nullable&lt;enum&gt; in every
/// assembly automatically, present or future. This global rule takes precedence over any per-pair
/// <c>NewConfig&lt;string, TEnum?&gt;()</c> registered for the same type pair, regardless of
/// registration order — a service should not need to override nullable-enum handling, but if one
/// ever does, know that an explicit registration will be silently ignored in favor of this rule.
/// </remarks>
public static class NullableEnumMapsterRegistration
{
    private static readonly MethodInfo ParseMethod =
        typeof(NullableEnumMapsterRegistration).GetMethod(nameof(ParseNullableEnum), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"{nameof(ParseNullableEnum)} method not found via reflection.");

    /// <summary>
    /// Registers the single global string-&gt;Nullable&lt;enum&gt; rule on <paramref name="config"/>. Call once
    /// per <see cref="TypeAdapterConfig"/> (already done centrally in <c>AddZgwMapster</c>) — no per-service
    /// or per-assembly registration is needed.
    /// </summary>
    public static void RegisterNullableEnumRule(this TypeAdapterConfig config)
    {
        var setter = config.When(
            (sourceType, destinationType, _) => sourceType == typeof(string) && Nullable.GetUnderlyingType(destinationType)?.IsEnum == true
        );

        setter.Settings.ConverterFactory = BuildConverter;
        // MapType.MapToTarget (mapping onto an EXISTING destination object, e.g. an EF Core
        // update-in-place `mapper.Map(source, existingEntity)`) uses ConverterToTargetFactory, not
        // ConverterFactory. Without this, MapToTarget silently falls back to Mapster's own default
        // nullable-enum handling and reproduces the exact silent zero-value-substitution bug this
        // rule exists to prevent. The destination parameter is unused (this conversion doesn't need
        // the prior value) — it exists only to match the Func<TSource,TDestination,TDestination>
        // shape Mapster expects for MapToTarget, mirroring how Mapster's own MapWith/MapToTargetWith
        // build both factories from the same underlying expression.
        setter.Settings.ConverterToTargetFactory = BuildConverterToTarget;
    }

    private static LambdaExpression BuildConverter(CompileArgument arg)
    {
        var enumType = Nullable.GetUnderlyingType(arg.DestinationType)!;
        var method = ParseMethod.MakeGenericMethod(enumType);
        var source = Expression.Parameter(typeof(string), "source");
        return Expression.Lambda(Expression.Call(method, source), source);
    }

    private static LambdaExpression BuildConverterToTarget(CompileArgument arg)
    {
        var enumType = Nullable.GetUnderlyingType(arg.DestinationType)!;
        var method = ParseMethod.MakeGenericMethod(enumType);
        var source = Expression.Parameter(typeof(string), "source");
        var destination = Expression.Parameter(arg.DestinationType, "destination");
        return Expression.Lambda(Expression.Call(method, source), source, destination);
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
}
