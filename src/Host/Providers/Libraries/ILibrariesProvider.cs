using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Core.Persistence.Models.Libraries;
using KaraW3B.Server.Songs.Models.Libraries;

namespace KaraW3B.Server.Songs.Host.Providers.Libraries
{
    public interface ILibrariesProvider
    {
        IAsyncEnumerable<Library> GetLibrariesAsync(CancellationToken cancellationToken);
        Task<DbLibrary> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken);
        Task<Library> CreateLibraryAsync(LibraryCreationPayload payload, CancellationToken cancellationToken);
        Task<bool> DeleteLibraryAsync(Guid libraryId, CancellationToken cancellationToken);

        Task StartLibraryAnalyzeAsync(DbLibrary library, LibraryAnalyzeType analyzeType,
            CancellationToken cancellationToken);
    }
}