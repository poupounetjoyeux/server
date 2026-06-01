using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Core.Models.Songs;
using KaraWeb.Core.Models.Songs.Notes;

namespace KaraWeb.Host.Providers.Songs
{
    public interface ISongsProvider
    {
        IAsyncEnumerable<Song> GetSongsByCollection(Collection collection, CancellationToken cancellationToken);
        Task<Song> GetSong(Guid songId, CancellationToken cancellationToken);
        IAsyncEnumerable<SongNote> GetSongNotes(Song song, CancellationToken cancellationToken);
    }
}
