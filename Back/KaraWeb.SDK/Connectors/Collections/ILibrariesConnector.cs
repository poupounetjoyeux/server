using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Shared.Models.Libraries;
using KaraWeb.Shared.Models.Songs;

namespace KaraWeb.SDK.Connectors.Collections
{
    public interface ILibrariesConnector
    {
        IAsyncEnumerable<LibraryDto> GetLibrariesAsync(CancellationToken cancellationToken = default);
        Task<LibraryDto> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default);
        IAsyncEnumerable<SongDto> GetSongsAsync(Guid libraryId, bool withErrors, CancellationToken cancellationToken = default);
    }
}
