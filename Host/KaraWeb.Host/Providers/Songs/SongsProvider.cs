using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Core.Models.Songs;
using KaraWeb.Core.Models.Songs.Notes;
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

        public IAsyncEnumerable<Song> GetSongsByCollection(Collection collection, CancellationToken cancellationToken)
        {
            return _dbContext.Songs.Where(s => s.CollectionId == collection.Id).ToAsyncEnumerable();
        }

        public Task<Song> GetSong(Guid songId, CancellationToken cancellationToken)
        {
            return _dbContext.Songs.SingleOrDefaultAsync(s => s.Id == songId, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<SongNote> GetSongNotes(Song song, CancellationToken cancellationToken)
        {
            return _dbContext.SongNotes.Where(n => n.SongId == song.Id).OrderBy(n => n.PlayerNumber).ThenBy(n => n.StartBeat).ToAsyncEnumerable();
        }
    }
}
