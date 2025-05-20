using System;
using System.Globalization;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.JsonConverters;

public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly? ReadJson(JsonReader reader, Type objectType, DateOnly? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value != null)
        {
            return DateOnly.ParseExact((string)reader.Value, DateFormat, CultureInfo.InvariantCulture);
        }
        return null;
    }

    public override void WriteJson(JsonWriter writer, DateOnly? value, JsonSerializer serializer)
    {
        writer.WriteValue(value.HasValue ? value.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : null);
    }
}
