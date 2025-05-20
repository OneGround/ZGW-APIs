using System;
using AutoMapper;
using AutoMapper.Internal;
using AutoMapper.Internal.Mappers;

namespace OneGround.ZGW.Common.Web;

/// <summary>
/// This class ensures that empty string "" will map to null value for Nullable enum types.
/// </summary>
public class NullableEnumMapper : ObjectMapper<string, object>
{
    public override bool IsMatch(TypePair context)
    {
        var underlyingType = Nullable.GetUnderlyingType(context.DestinationType);

        return underlyingType is { IsEnum: true } && context.SourceType == typeof(string);
    }

    public override object Map(string source, object destination, Type sourceType, Type destinationType, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source))
            return null;

        var underlyingType = Nullable.GetUnderlyingType(destinationType);

        if (Enum.IsDefined(underlyingType, source) && Enum.TryParse(underlyingType, source, out var result))
            return result;

        return null;
    }
}
