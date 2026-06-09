using System;
using System.Net.Http;
using KaraWeb.SDK.Connectors.Collections;
using KaraWeb.SDK.Connectors.Songs;

namespace KaraWeb.SDK.Connectors
{
    public sealed class KaraWebConnector : IKaraWebConnector, IDisposable
    {
        private readonly HttpClient _httpClient;

        public KaraWebConnector(Uri baseUri, TimeSpan? timeout = null)
        {
            _httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(30) };

            Libraries = new LibrariesConnector(_httpClient, baseUri);
            Songs = new SongsConnector(_httpClient, baseUri);
        }

        public ILibrariesConnector Libraries { get; }

        public ISongsConnector Songs { get; }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
