using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Models.Songs;

namespace KaraWeb.Core.Services.SongParser
{
    public interface ISongParserService
    {
        Task<bool> ParseSongAsync(FileInfo songFile, Song songToUpdate,
            CancellationToken cancellationToken);
    }
}