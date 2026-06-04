using System;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Host.Providers.Songs;
using KaraWeb.Shared.Models.Songs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KaraWeb.Host.Controllers
{
    [Route(Name)]
    public sealed class SongsController : ControllerBase
    {
        public const string Name = "songs";

        private readonly ISongsProvider _songsProvider;

        public SongsController(ISongsProvider songsProvider)
        {
            _songsProvider = songsProvider;
        }

        /// <summary>
        ///     Get detailed (containing notes, errors, warnings, etc...) song by its ID
        /// </summary>
        /// <param name="songId">The ID of the song to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{songId}")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked song", typeof(DetailedSongDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No song found with the given ID", typeof(string))]
        public async Task<ActionResult<DetailedSongDto>> GetSongAsync([FromRoute] Guid songId,
            CancellationToken cancellationToken = default)
        {
            var song = await _songsProvider.GetDetailedSongAsync(songId, cancellationToken);
            if (song == null)
            {
                return NotFound($"The song with ID {songId} doesn't exist");
            }

            return Ok(song);
        }
    }
}