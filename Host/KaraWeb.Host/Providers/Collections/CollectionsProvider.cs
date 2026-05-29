using KaraWeb.Core;
using KaraWeb.Core.Models;
using KaraWeb.Host.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.Host.Providers.Collections
{
    public class CollectionsProvider : ICollectionsProvider
    {
        private KaraWebDbContext _dbContext;

        public CollectionsProvider(KaraWebDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IAsyncEnumerable<Collection> GetCollectionsAsync(CancellationToken cancellationToken)
        {
            return _dbContext.Collections.ToAsyncEnumerable();
        }

        public Task<Collection> GetCollectionAsync(Guid collectionId, CancellationToken cancellationToken)
        {
            return _dbContext.Collections.Where(c => c.Id == collectionId).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Collection> CreateCollectionAsync(CollectionPayload payload, CancellationToken cancellationToken)
        {
            var newCollection = new Collection
            {
                Id = Guid.NewGuid(),
                Name = payload.Name,
                Decription = payload.Description,
                Path = payload.Path
            };
            var collectionEntry = await _dbContext.Collections.AddAsync(newCollection);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return collectionEntry.Entity;
        }

        public async Task DeleteCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            _dbContext.Collections.Remove(collection);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
