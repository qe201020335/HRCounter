using System;
using IPA.Config.Data;
using IPA.Config.Stores;
using Newtonsoft.Json;

namespace HRCounter.Utils.Converters;

public class WrappedTextValueJsonConverter<TValue, TValueConverter> : JsonConverter<TValue> where TValueConverter : ValueConverter<TValue>, new()
{
    private readonly TValueConverter _converter = new();

    public override void WriteJson(JsonWriter writer, TValue? value, JsonSerializer serializer)
    {
        var textValue = (Text?)_converter.ToValue(value, null!);

        if (textValue == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(textValue.Value);
    }

    public override TValue? ReadJson(JsonReader reader, Type objectType, TValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return default;
        }

        if (reader.TokenType != JsonToken.String)
        {
            return hasExistingValue ? existingValue : default;
        }

        var textValue = new Text(reader.Value!.ToString());
        return _converter.FromValue(textValue, null!);
    }
}
