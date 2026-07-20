using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaraW3B.Server.Songs.Models.JsonConverters
{
    public sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TryGetDouble(out var ms) ? TimeSpan.FromMilliseconds(ms) : TimeSpan.Zero;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalMilliseconds);
        }
    }
}