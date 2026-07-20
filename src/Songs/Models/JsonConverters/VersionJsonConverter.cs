using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaraW3B.Server.Songs.Models.JsonConverters
{
    public sealed class VersionJsonConverter : JsonConverter<Version>
    {
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var versionString = reader.GetString();
            return string.IsNullOrEmpty(versionString) ? null : Version.Parse(versionString);
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString(3));
        }
    }
}