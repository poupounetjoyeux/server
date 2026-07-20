using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Models.Libraries;
using KaraW3B.Server.Songs.Models.Songs;

namespace KaraW3B.Client.Songs.Connectors.Collections
{
    public interface ILibrariesConnector
    {
        IAsyncEnumerable<Library> GetLibrariesAsync(CancellationToken cancellationToken = default);
        Task<Library> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Song> GetSongsAsync(Guid libraryId, bool onlyLoadableSongs,
            CancellationToken cancellationToken = default);
    }
}