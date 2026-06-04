using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Libraries;
using KaraWeb.Core.Services.LibrariesAnalyzer;
using KaraWeb.Shared.Models.Libraries;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Host.Providers.Libraries
{
    internal sealed class LibrariesProvider : ILibrariesProvider
    {
        private readonly KaraWebDbContext _dbContext;
        private readonly ILibrariesAnalyzerService _librariesAnalyzerService;

        public LibrariesProvider(KaraWebDbContext dbContext, ILibrariesAnalyzerService librariesAnalyzerService)
        {
            _dbContext = dbContext;
            _librariesAnalyzerService = librariesAnalyzerService;
        }

        public async IAsyncEnumerable<LibraryDto> GetLibrariesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var library in _dbContext.Libraries.ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return library.ToDto();
            }
        }

        public async Task<LibraryDto> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            return (await GetLibraryInternalAsync(libraryId, cancellationToken)).ToDto();
        }

        public async Task<LibraryDto> CreateLibraryAsync(LibraryCreationPayload payload,
            CancellationToken cancellationToken)
        {
            var newLibrary = new Library
            {
                Name = payload.Name,
                Description = payload.Description,
                Path = payload.Path
            };
            var libraryEntry = await _dbContext.Libraries.AddAsync(newLibrary, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return libraryEntry.Entity.ToDto();
        }

        public Task StartLibraryAnalyzeAsync(LibraryDto library, LibraryAnalyzeType libraryAnalyzeType,
            CancellationToken cancellationToken)
        {
            return _librariesAnalyzerService.StartLibraryAnalyzeAsync(library, libraryAnalyzeType, cancellationToken);
        }

        private Task<Library> GetLibraryInternalAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            return _dbContext.Libraries.SingleOrDefaultAsync(c => c.Id == libraryId, cancellationToken);
        }

        public async Task<bool> DeleteLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            var libraryToDelete = await GetLibraryInternalAsync(libraryId, cancellationToken);
            if (libraryToDelete == null)
            {
                return false;
            }

            var songs = await _dbContext.Songs.Where(s => s.LibraryId == libraryId).ToListAsync(cancellationToken);
            _dbContext.RemoveRange(songs);
            _dbContext.Libraries.Remove(libraryToDelete);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}