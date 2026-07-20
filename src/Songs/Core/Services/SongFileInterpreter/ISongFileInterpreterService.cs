using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Core.Persistence.Models.Songs;

namespace KaraW3B.Server.Songs.Core.Services.SongFileInterpreter
{
    public interface ISongFileInterpreterService
    {
        Task<bool> ParseSongAndCheckAsync(FileInfo songFile, DbSong songToUpdate,
            CancellationToken cancellationToken);

        Task WriteSongFile(DbSong songToWrite, string filePath, bool overwrite, CancellationToken cancellationToken);
    }
}