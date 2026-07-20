using System.Text.Json;
using System.Text.Json.Serialization;
using KaraW3B.Server.Songs.Models.JsonConverters;

namespace KaraW3B.Server.Songs.Models.Helpers
{
    public static class JsonHelper
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions =
            ConfigureJsonSerializer(new JsonSerializerOptions());

        public static JsonSerializerOptions ConfigureJsonSerializer(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.WriteIndented = true;
            options.AllowTrailingCommas = true;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new VersionJsonConverter());
            options.Converters.Add(new TimeSpanJsonConverter());
            options.Converters.Add(new TimeSpanNullableJsonConverter());
            return options;
        }
    }
}