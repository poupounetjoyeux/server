using System;
using System.Net.Http;
using KaraW3B.Client.Songs.Connectors.Collections;
using KaraW3B.Client.Songs.Connectors.Songs;
using KaraW3B.Client.Songs.Helpers;

namespace KaraW3B.Client.Songs.Connectors
{
    public sealed class KaraW3BConnector : IKaraW3BConnector, IDisposable
    {
        private readonly HttpClient _httpClient;

        public KaraW3BConnector(Uri serverUri, TimeSpan? timeout = null)
        {
            _httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(30) };

            var baseApiUri = serverUri.AppendPath("api");
            Libraries = new LibrariesConnector(_httpClient, baseApiUri);
            Songs = new SongsConnector(_httpClient, baseApiUri);
        }

        public ILibrariesConnector Libraries { get; }

        public ISongsConnector Songs { get; }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}