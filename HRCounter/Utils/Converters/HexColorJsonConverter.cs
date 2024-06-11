using System;
using Newtonsoft.Json;
using UnityEngine;

namespace HRCounter.Utils.Converters
{
    public class HexColorJsonConverter: JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue($"#{ColorUtility.ToHtmlStringRGB(value)}");
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value is string hex)
            {
                if (ColorUtility.TryParseHtmlString(hex, out var color))
                {
                    return color;
                }
            }

            return existingValue;
        }
    }
}