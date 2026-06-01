using KaraWeb.Core.Helpers;
using KaraWeb.Core.Models.Songs;
using KaraWeb.Host.Providers.Songs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Models.Songs.Notes;

namespace KaraWeb.Host.Controllers
{
    [Route(Constants.ApiMainRoutePrefix + Name)]
    public sealed class SongsController : ControllerBase
    {
        public const string Name = "songs";

        private readonly ISongsProvider _songsProvider;

        public SongsController(ISongsProvider songsProvider)
        {
            _songsProvider = songsProvider;
        }

        /// <summary>
        /// Get song by its ID
        /// </summary>
        /// <param name="songId">The ID of the song to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{songId}")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked song", typeof(Song))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No song found with the given ID", typeof(string))]
        public async Task<ActionResult<Song>> GetSongAsync([FromRoute] Guid songId, CancellationToken cancellationToken = default)
        {
            var song = await _songsProvider.GetSong(songId, cancellationToken);
            if (song == null)
            {
                return NotFound($"The song with ID {songId} doesn't exist");
            }

            return Ok(song);
        }

        /// <summary>
        /// Get song's notes
        /// </summary>
        /// <param name="songId">The ID of the song to get notes from</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{songId}/notes")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked song's notes", typeof(List<SongNote>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No song found with the given ID", typeof(string))]
        public async Task<ActionResult<List<Song>>> GetCollectionSongsAsync([FromRoute] Guid songId, CancellationToken cancellationToken = default)
        {
            var song = await _songsProvider.GetSong(songId, cancellationToken);
            if (song == null)
            {
                return NotFound($"The song with ID {songId} doesn't exist");
            }

            return Ok(await _songsProvider.GetSongNotes(song, cancellationToken).ToListAsync(cancellationToken));
        }
    }
}
