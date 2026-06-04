using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Notes;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Host.Providers.Songs
{
    internal sealed class SongsProvider : ISongsProvider
    {
        private readonly KaraWebDbContext _dbContext;

        public SongsProvider(KaraWebDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async IAsyncEnumerable<SongDto> GetSongsByLibraryAsync(Guid libraryId, bool withErrors,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var song in _dbContext.Songs
                               .Where(s => s.LibraryId == libraryId && (withErrors || s.Errors.Count == 0))
                               .ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return song.ToDto();
            }
        }

        public async Task<DetailedSongDto> GetDetailedSongAsync(Guid songId, CancellationToken cancellationToken)
        {
            return (await GetSongInternalAsync(songId, cancellationToken)).ToDetailedDto();
        }

        public async IAsyncEnumerable<SongNoteDto> GetSongNotesAsync(Guid songId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var song = await GetSongInternalAsync(songId, cancellationToken);
            if (song == null)
            {
                yield break;
            }

            foreach (var note in song.Notes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return note.ToDto();
            }
        }

        public Task<Song> GetSongInternalAsync(Guid songId, CancellationToken cancellationToken)
        {
            return _dbContext.Songs.SingleOrDefaultAsync(s => s.Id == songId, cancellationToken);
        }
    }
}