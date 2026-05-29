using KaraWeb.Core.Helpers;
using KaraWeb.Core.Models;
using KaraWeb.Host.Models;
using KaraWeb.Host.Providers.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.Host.Controllers
{
    [Route(Constants.ApiMainRoutePrefix + "collections")]
    public class CollectionsController : ControllerBase
    {
        private ICollectionsProvider _collectionsProvider;

        public CollectionsController(ICollectionsProvider collectionsProvider)
        {
            _collectionsProvider = collectionsProvider;
        }

        /// <summary>
        /// Get all song collections
        /// </summary>
        /// <param name="cancellationToken"></param>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "The set of all collections", typeof(List<Collection>))]
        public async Task<ActionResult<List<Collection>>> GetAllCollectionsAsync(CancellationToken cancellationToken = default)
        {
            return Ok(await _collectionsProvider.GetCollectionsAsync(cancellationToken).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Get song collection by its ID
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

            return Created(uri: string.Empty, value: await _collectionsProvider.CreateCollectionAsync(newCollection, cancellationToken));
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
