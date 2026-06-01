using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Core.Models.Jobs;
using KaraWeb.Core.Services.SongParser;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Services.CollectionsAnalyzer
{
    public sealed class CollectionsAnalyzerService : ICollectionsAnalyzerService
    {
        private readonly ISongParserService _songParserService;
        private readonly KaraWebDbContext _dbContext;

        public CollectionsAnalyzerService(ISongParserService songParserService, KaraWebDbContext dbContext)
        {
            _songParserService = songParserService;
            _dbContext = dbContext;
        }

        public async Task<Job> StartCollectionAnalyzeAsync(Collection collection, CancellationToken cancellationToken)
        {
            // TODO: Make it background
            var job = new Job { JobId = Guid.NewGuid(), Status = JobStatus.Processing };

            var directory = new DirectoryInfo(collection.Path);
            if (!directory.Exists)
            {
                job.Status = JobStatus.Error;
                job.ResultMessage = $"Directory '{directory.FullName}' doesn't exist";
                return job;
            }

            await Parallel.ForEachAsync(directory.GetFiles("*.txt", SearchOption.AllDirectories), cancellationToken, (f, c) => ParseSongFile(collection.Id, f, c));
            job.ResultMessage = $"Collection {collection.Name} parsed successfully";
            return job;
        }

        private async ValueTask ParseSongFile(Guid collectionId, FileInfo songFile, CancellationToken cancellationToken)
        {
            var parsingResult = await _songParserService.ParseSongAsync(collectionId, songFile, cancellationToken);

            if (parsingResult.IsSuccess)
            {
                var existingSong = await _dbContext.Songs.SingleOrDefaultAsync(s =>
                    s.SongFilePath == parsingResult.ParsedSong.SongFilePath &&
                    s.CollectionId == parsingResult.ParsedSong.CollectionId, cancellationToken: cancellationToken);
                if (existingSong != null)
                {
                    _dbContext.Songs.Remove(existingSong);
                    var existingNotes = await _dbContext.SongNotes.Where(n => n.SongId == existingSong.Id).ToListAsync(cancellationToken);
                    _dbContext.SongNotes.RemoveRange(existingNotes);
                }

                await _dbContext.Songs.AddAsync(parsingResult.ParsedSong, cancellationToken);
                await _dbContext.SongNotes.AddRangeAsync(parsingResult.Notes, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
