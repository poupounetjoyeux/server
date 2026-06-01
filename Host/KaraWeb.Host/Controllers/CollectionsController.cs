using System;
using KaraWeb.Core.Helpers;
using KaraWeb.Host.Models;
using KaraWeb.Host.Providers.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Host.Providers.Songs;
using KaraWeb.Core.Models.Songs;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Core.Models.Jobs;

namespace KaraWeb.Host.Controllers
{
    [Route(Constants.ApiMainRoutePrefix + Name)]
    public sealed class CollectionsController : ControllerBase
    {
        public const string Name = "collections";

        private readonly ICollectionsProvider _collectionsProvider;
        private readonly ISongsProvider _songsProvider;

        public CollectionsController(ICollectionsProvider collectionsProvider, ISongsProvider songsProvider)
        {
            _collectionsProvider = collectionsProvider;
            _songsProvider = songsProvider;
        }

        /// <summary>
        /// Get all collections
        /// </summary>
        /// <param name="cancellationToken"></param>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "The set of all collections", typeof(List<Collection>))]
        public async Task<ActionResult<List<Collection>>> GetAllCollectionsAsync(CancellationToken cancellationToken = default)
        {
            return Ok(await _collectionsProvider.GetCollectionsAsync(cancellationToken).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Get collection by its ID
        /// </summary>
        /// <param name="collectionId">The ID of the collection to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{collectionId}")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked collection", typeof(Collection))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No collection found with the given ID", typeof(string))]
        public async Task<ActionResult<Collection>> GetCollectionAsync([FromRoute] Guid collectionId, CancellationToken cancellationToken = default)
        {
            var collection = await _collectionsProvider.GetCollectionAsync(collectionId, cancellationToken);
            if(collection == null)
            {
                return NotFound($"The collection with ID {collectionId} doesn't exist");
            }

            return Ok(collection);
        }

        /// <summary>
        /// Get collection's songs
        /// </summary>
        /// <param name="collectionId">The ID of the collection to get songs from</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{collectionId}/songs")]
        [SwaggerResponse(StatusCodes.Status200OK, "The asked collection's songs", typeof(List<Song>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No collection found with the given ID", typeof(string))]
        public async Task<ActionResult<List<Song>>> GetCollectionSongsAsync([FromRoute] Guid collectionId, CancellationToken cancellationToken = default)
        {
            var collection = await _collectionsProvider.GetCollectionAsync(collectionId, cancellationToken);
            if(collection == null)
            {
                return NotFound($"The collection with ID {collectionId} doesn't exist");
            }

            return Ok(await _songsProvider.GetSongsByCollection(collection, cancellationToken).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Create a new song collection
        /// </summary>
        /// <param name="newCollection">The collection payload to create</param>
        /// <param name="cancellationToken"></param>
        [HttpPut]
        [SwaggerResponse(StatusCodes.Status201Created, "The created collection", typeof(Collection))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The given payload is invalid", typeof(string))]
        public async Task<ActionResult<Collection>> CreateCollectionAsync([FromBody] CollectionPayload newCollection, CancellationToken cancellationToken = default)
        {
            if(!newCollection.IsValid(out var error))
            {
                return BadRequest(error);
            }

            var createdCollection = await _collectionsProvider.CreateCollectionAsync(newCollection, cancellationToken);
            return Created(uri: $"{Name}/{createdCollection.Id}", value: createdCollection);
        }

        /// <summary>
        /// Start a collection analyze by its ID
        /// </summary>
        /// <param name="collectionId">The ID of the collection to analyze</param>
        /// <param name="cancellationToken"></param>
        [HttpPost("{collectionId}/do-start-analyze")]
        [SwaggerResponse(StatusCodes.Status202Accepted, "The created analyze job", typeof(Job))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No collection found with the given ID", typeof(string))]
        public async Task<ActionResult<Job>> StartCollectionAnalyzeAsync([FromRoute] Guid collectionId, CancellationToken cancellationToken = default)
        {
            var collection = await _collectionsProvider.GetCollectionAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound($"The collection with ID {collectionId} doesn't exist");
            }


            return Ok(await _collectionsProvider.StartCollectionAnalyzeAsync(collection, cancellationToken));
        }

        /// <summary>
        /// Delete an existing song collection by its ID
        /// </summary>
        /// <param name="collectionId">The ID of the collection to retrieve</param>
        /// <param name="cancellationToken"></param>
        [HttpDelete("{collectionId}")]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No collection found with the given ID", typeof(string))]
        public async Task<ActionResult<Collection>> DeleteCollectionAsync([FromRoute] Guid collectionId, CancellationToken cancellationToken = default)
        {
            var collection = await _collectionsProvider.GetCollectionAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                return NotFound($"The collection with ID {collectionId} doesn't exist");
            }

            await _collectionsProvider.DeleteCollectionAsync(collection, cancellationToken);
            return NoContent();
        }
    }
}
