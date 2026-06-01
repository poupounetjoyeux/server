using System;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Host.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Models.Jobs;

namespace KaraWeb.Host.Providers.Collections
{
    public interface ICollectionsProvider
    {
        IAsyncEnumerable<Collection> GetCollectionsAsync(CancellationToken cancellationToken);
        Task<Collection> GetCollectionAsync(Guid collectionId, CancellationToken cancellationToken);
        Task<Collection> CreateCollectionAsync(CollectionPayload payload, CancellationToken cancellationToken);
        Task DeleteCollectionAsync(Collection collection, CancellationToken cancellationToken);
        Task<Job> StartCollectionAnalyzeAsync(Collection collection, CancellationToken cancellationToken);
    }
}
