using KaraWeb.SDK.Helpers;
using KaraWeb.Shared.Exceptions;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Files;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.SDK.Connectors.Songs
{
    internal sealed class SongsConnector : ISongsConnector
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _baseUri;

        public SongsConnector(HttpClient httpClient, Uri baseUri)
        {
            _httpClient = httpClient;
            _baseUri = baseUri.AppendPath("songs");
        }

        public async Task<DetailedSongDto> GetSongDetailsAsync(Guid songId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}/details"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraWebException(
                    $"Unable to get song details: {await response.Content.ReadAsStringAsync()}");
            }

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<DetailedSongDto>(responseStream
                , JsonHelper.DefaultJsonSerializerOptions, cancellationToken);
        }

        public async Task<Stream> GetSongFileStreamAsync(Guid songId, FileType fileType, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}/streams/{fileType}"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraWebException(
                    $"Unable to get song file stream {fileType}: {await response.Content.ReadAsStringAsync()}");
            }

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
