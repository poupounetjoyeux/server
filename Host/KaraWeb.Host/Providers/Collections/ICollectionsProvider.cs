using KaraWeb.Core.Models;
using KaraWeb.Host.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.Host.Providers.Collections
{
    public interface ICollectionsProvider
    {
        IAsyncEnumerable<Collection> GetCollectionsAsync(CancellationToken cancellationToken);
        Task<Collection> GetCollectionAsync(Guid collectionId, CancellationToken cancellationToken);
        Task<Collection> CreateCollectionAsync(CollectionPayload payload, CancellationToken cancellationToken);
        Task DeleteCollectionAsync(Collection collection, CancellationToken cancellationToken);
    }
}
