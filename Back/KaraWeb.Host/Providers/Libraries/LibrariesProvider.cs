using KaraWeb.Core.Jobs;
using KaraWeb.Core.Persistence;
using KaraWeb.Core.Services.SchedulerService;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Shared.Models.Libraries;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Models.Libraries;

namespace KaraWeb.Host.Providers.Libraries
{
    internal sealed class LibrariesProvider : ILibrariesProvider
    {
        private readonly KaraWebDbContext _dbContext;
        private readonly ISongParserService _songParserService;
        private readonly ISchedulerService _schedulerService;

        public LibrariesProvider(KaraWebDbContext dbContext,
            ISongParserService songParserService, ISchedulerService schedulerService)
        {
            _dbContext = dbContext;
            _songParserService = songParserService;
            _schedulerService = schedulerService;
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

        public Task<Library> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            return _dbContext.Libraries.SingleOrDefaultAsync(c => c.Id == libraryId, cancellationToken);
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

        public async Task<bool> DeleteLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            var deleteCount = await _dbContext.Songs.Where(s => s.LibraryId == libraryId)
                .ExecuteDeleteAsync(cancellationToken);
            deleteCount += await _dbContext.Libraries.Where(l => l.Id == libraryId)
                .ExecuteDeleteAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return deleteCount > 0;
        }

        public Task StartLibraryAnalyzeAsync(Library library, LibraryAnalyzeType analyzeType,
            CancellationToken cancellationToken)
        {
            var dataMap = new JobDataMap
            {
                [AnalyzeLibraryJob.LibraryKey] = library,
                [AnalyzeLibraryJob.AnalyzeTypeKey] = analyzeType,
                [AnalyzeLibraryJob.SongParserServiceKey] = _songParserService,
            };
            return _schedulerService.StartJob(AnalyzeLibraryJob.JobKey, dataMap, cancellationToken);
        }
    }
}