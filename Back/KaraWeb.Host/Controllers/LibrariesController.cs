using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Host.Providers.Libraries;
using KaraWeb.Host.Providers.Songs;
using KaraWeb.Shared.Models.Libraries;
using KaraWeb.Shared.Models.Songs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KaraWeb.Host.Controllers
{
    [Route(Name)]
    public sealed class LibrariesController : ControllerBase
    {
        public const string Name = "libraries";

        private readonly ILibrariesProvider _librariesProvider;
        private readonly ISongsProvider _songsProvider;

        public LibrariesController(ILibrariesProvider librariesProvider, ISongsProvider songsProvider)
        {
            _librariesProvider = librariesProvider;
            _songsProvider = songsProvider;
        }

        /// <summary>
        ///     Get all libraries
        /// </summary>
        /// <param name="cancellationToken"></param>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "The set of all libraries", typeof(List<LibraryDto>))]
        public async Task<ActionResult<List<LibraryDto>>> GetAllLibrariesAsync(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _librariesProvider.GetLibrariesAsync(cancellationToken).ToListAsync(cancellationToken));
        }

        /// <summary>
        ///     Get library by its ID
        /// </summary>
        /// <param name="libraryId">The ID of the library to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{libraryId}")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked library", typeof(LibraryDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No library found with the given ID", typeof(string))]
        public async Task<ActionResult<LibraryDto>> GetLibraryAsync([FromRoute] Guid libraryId,
            CancellationToken cancellationToken = default)
        {
            var library = await _librariesProvider.GetLibraryAsync(libraryId, cancellationToken);
            if (library == null)
            {
                return NotFound($"The library with ID {libraryId} doesn't exist");
            }

            return Ok(library.ToDto());
        }

        /// <summary>
        ///     Get library's songs
        /// </summary>
        /// <param name="libraryId">The ID of the library to get songs from</param>
        /// <param name="withErrors">A filter to retrieve songs with errors (default: false)</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{libraryId}/songs")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked library's songs", typeof(List<SongDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No library found with the given ID", typeof(string))]
        public async Task<ActionResult<List<SongDto>>> GetSongsByLibraryAsync([FromRoute] Guid libraryId,
            [FromQuery] bool withErrors = false, CancellationToken cancellationToken = default)
        {
            if (await _librariesProvider.GetLibraryAsync(libraryId, cancellationToken) == null)
            {
                return NotFound($"The library with ID {libraryId} doesn't exist");
            }

            return Ok(await _songsProvider.GetSongsByLibraryAsync(libraryId, withErrors, cancellationToken)
                .ToListAsync(cancellationToken));
        }

        /// <summary>
        ///     Create a new song library
        /// </summary>
        /// <param name="newLibraryCreation">The library payload to create</param>
        /// <param name="cancellationToken"></param>
        [HttpPut]
        [SwaggerResponse(StatusCodes.Status201Created, "The created library", typeof(LibraryDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The given payload is invalid", typeof(string))]
        public async Task<ActionResult<LibraryDto>> CreateLibraryAsync(
            [FromBody] LibraryCreationPayload newLibraryCreation,
            CancellationToken cancellationToken = default)
        {
            if (!newLibraryCreation.IsValid(out var error))
            {
                return BadRequest(error);
            }

            var createdLibrary = await _librariesProvider.CreateLibraryAsync(newLibraryCreation, cancellationToken);
            return Created($"{Name}/{createdLibrary.Id}", createdLibrary);
        }

        /// <summary>
        ///     Start a library analyze by its ID
        /// </summary>
        /// <param name="libraryId">The ID of the library to analyze</param>
        /// <param name="payload">Payload containing analyze options</param>
        /// <param name="cancellationToken"></param>
        [HttpPost("{libraryId}/do-start-analyze")]
        [SwaggerResponse(StatusCodes.Status202Accepted, "The created analyze job")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The given library is not ready to be analyzed", typeof(string))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No library found with the given ID", typeof(string))]
        public async Task<IActionResult> StartLibraryAnalyzeAsync([FromRoute] Guid libraryId,
            [FromBody] LibraryAnalyzePayload payload, CancellationToken cancellationToken = default)
        {
            var library = await _librariesProvider.GetLibraryAsync(libraryId, cancellationToken);
            if (library == null)
            {
                return NotFound($"The library with ID {libraryId} doesn't exist");
            }

            if (library.IsAnalyzing)
            {
                return BadRequest($"The library with ID {libraryId} is already analyzing");
            }

            await _librariesProvider.StartLibraryAnalyzeAsync(library, payload.AnalyzeType, cancellationToken);
            return Accepted();
        }

        /// <summary>
        ///     Delete an existing song library by its ID
        /// </summary>
        /// <param name="libraryId">The ID of the library to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpDelete("{libraryId}")]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No library found with the given ID", typeof(string))]
        public async Task<IActionResult> DeleteLibraryAsync([FromRoute] Guid libraryId,
            CancellationToken cancellationToken = default)
        {
            if (!await _librariesProvider.DeleteLibraryAsync(libraryId, cancellationToken))
            {
                return NotFound($"The library with ID {libraryId} doesn't exist");
            }

            return NoContent();
        }
    }
}