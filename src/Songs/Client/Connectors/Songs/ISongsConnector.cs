using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Models.Songs;
using KaraW3B.Server.Songs.Models.Songs.Alerts;
using KaraW3B.Server.Songs.Models.Songs.Notes;

namespace KaraW3B.Client.Songs.Connectors.Songs
{
    public interface ISongsConnector
    {
        Task<Song> GetSongAsync(Guid songId, CancellationToken cancellationToken = default);
        IAsyncEnumerable<SongNote> GetSongNotesAsync(Guid songId, CancellationToken cancellationToken = default);
        IAsyncEnumerable<SongAlert> GetSongAlertsAsync(Guid songId, CancellationToken cancellationToken = default);

        Task<Stream> GetSongFileStreamAsync(Guid songId, FileType fileType,
            CancellationToken cancellationToken = default);
    }
}