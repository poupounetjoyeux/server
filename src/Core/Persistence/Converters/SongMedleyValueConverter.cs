using System.Text.Json;
using KaraW3B.Server.Songs.Core.Persistence.Models.Songs;
using KaraW3B.Server.Songs.Models.Helpers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KaraW3B.Server.Songs.Core.Persistence.Converters
{
    internal sealed class SongMedleyValueConverter : ValueConverter<DbSongMedley, string>
    {
        public SongMedleyValueConverter() : base(
            s => s == null ? null : JsonSerializer.Serialize(s, JsonHelper.DefaultJsonSerializerOptions),
            s => string.IsNullOrEmpty(s)
                ? null
                : JsonSerializer.Deserialize<DbSongMedley>(s, JsonHelper.DefaultJsonSerializerOptions))
        {
        }
    }
}