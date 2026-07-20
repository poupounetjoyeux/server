using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Client.Songs.Helpers;
using KaraW3B.Server.Songs.Models.Helpers;
using KaraW3B.Server.Songs.Models.Libraries;
using KaraW3B.Server.Songs.Models.Songs;

namespace KaraW3B.Client.Songs.Connectors.Collections
{
    internal sealed class LibrariesConnector : ILibrariesConnector
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _baseUri;

        public LibrariesConnector(HttpClient httpClient, Uri baseUri)
        {
            _httpClient = httpClient;
            _baseUri = baseUri.AppendPath("libraries");
        }

        public async IAsyncEnumerable<Library> GetLibrariesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(_baseUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get libraries: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var libraries = JsonSerializer.DeserializeAsyncEnumerable<Library>(
                responseStream, JsonHelper.DefaultJsonSerializerOptions, cancellationToken);
            await foreach (var library in libraries)
            {
                yield return library;
            }
        }

        public async Task<Library> GetLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath(libraryId.ToString()), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get library: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<Library>(
                responseStream, JsonHelper.DefaultJsonSerializerOptions, cancellationToken);
        }

        public async IAsyncEnumerable<Song> GetSongsAsync(Guid libraryId, bool onlyLoadableSongs,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(_baseUri.AppendPath($"{libraryId}/songs"))
            {
                Query = $"onlyLoadableSongs={onlyLoadableSongs}"
            };

            var response = await _httpClient.GetAsync(uriBuilder.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get songs for library: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var songs = JsonSerializer.DeserializeAsyncEnumerable<Song>(
                responseStream, JsonHelper.DefaultJsonSerializerOptions, cancellationToken);
            await foreach (var song in songs)
            {
                yield return song;
            }
        }
    }
}