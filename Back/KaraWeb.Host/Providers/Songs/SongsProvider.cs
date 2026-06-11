using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Models.Songs;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Files;
using KaraWeb.Shared.Models.Songs.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Host.Providers.Songs
{
    internal sealed class SongsProvider : ISongsProvider
    {
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider = new();
        private readonly KaraWebDbContext _dbContext;

        public SongsProvider(KaraWebDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async IAsyncEnumerable<SongDto> GetSongsByLibraryAsync(Guid libraryId, bool withErrors,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var song in _dbContext.Songs
                               .Where(s => s.LibraryId == libraryId)
                               .OrderBy(s => s.Artist)
                               .ThenBy(s => s.Title)
                               .ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!withErrors && song.Alerts.Any(a => a.Level == AlertLevel.Error))
                {
                    continue;
                }

                yield return song.ToDto();
            }
        }

        public async Task<DetailedSongDto> GetDetailedSongAsync(Guid songId, CancellationToken cancellationToken)
        {
            var song = await GetSongById(songId, cancellationToken);
            return song?.ToDetailedDto();
        }

        public Task<Song> GetSongById(Guid songId, CancellationToken cancellationToken)
        {
            return _dbContext.Songs.SingleOrDefaultAsync(s => s.Id == songId, cancellationToken);
        }

        public Task<FileStreamResult> GetSongFileStream(Song song, FileType fileType,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var filePath = song.GetSongFilePath(fileType);

                var contentType = "application/octet-stream";
                if (_fileExtensionContentTypeProvider.TryGetContentType(filePath, out var gotContentType))
                {
                    contentType = gotContentType;
                }

                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return new FileStreamResult(stream, contentType);
            }, cancellationToken);
        }
    }
}