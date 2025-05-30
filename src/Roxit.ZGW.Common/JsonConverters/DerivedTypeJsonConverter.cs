using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Common.Exceptions;

namespace Roxit.ZGW.Common.JsonConverters;

public abstract class DerivedTypeJsonConverter<TBase> : JsonConverter
{
    /// <summary>
    /// Returns the type that corresponds to the specified type value.
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    protected abstract Type NameToType(string typeName);

    /// <summary>
    /// The name of the "type" property in JSON (set by derived class)
    /// </summary>
    protected abstract string TypePropertyName { get; }

    public override bool CanConvert(Type objectType)
    {
        return typeof(TBase) == objectType;
    }

    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);

        if (!obj.TryGetValue(TypePropertyName, StringComparison.InvariantCultureIgnoreCase, out var typeToken))
            return obj.ToObject<TBase>();

        if (typeToken.Type != JTokenType.String)
            return obj.ToObject<TBase>();

        var typeName = typeToken.Value<string>();
        var targetType = NameToType(typeName);

        if (targetType != null && typeof(TBase).IsAssignableFrom(targetType))
        {
            try
            {
                return obj.ToObject(targetType);
            }
            catch (Exception ex)
            {
                throw new ApiJsonParsingException(
                    $"Could not create an instance '{targetType.Name}' of base type '{objectType.Name}' due parsing content.",
                    "nonFieldErrors",
                    ex
                );
            }
        }

        throw new ApiJsonParsingException(
            $"Could not create an instance '{typeName}' of base type '{objectType.Name}'. Invalid or unauthorized type.",
            "nonFieldErrors"
        );
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}
