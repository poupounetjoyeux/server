using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaraW3B.Server.Songs.Models.JsonConverters
{
    public sealed class TimeSpanNullableJsonConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetDouble(out var ms))
            {
                return TimeSpan.FromMilliseconds(ms);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(value.Value.TotalMilliseconds);
        }
    }
}