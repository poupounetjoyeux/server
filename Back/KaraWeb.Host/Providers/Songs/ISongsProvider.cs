using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Shared.Models.Songs;

namespace KaraWeb.Host.Providers.Songs
{
    public interface ISongsProvider
    {
        IAsyncEnumerable<SongDto> GetSongsByLibraryAsync(Guid libraryId, bool withErrors,
            CancellationToken cancellationToken);
        Task<DetailedSongDto> GetDetailedSongAsync(Guid songId, CancellationToken cancellationToken);
    }
}