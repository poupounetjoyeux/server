using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Songs;

namespace KaraWeb.Core.Services.SongParser
{
    public interface ISongParserService
    {
        Task<Song> ParseSongAsync(Guid libraryId, FileInfo songFile, string fileHash,
            CancellationToken cancellationToken);
    }
}