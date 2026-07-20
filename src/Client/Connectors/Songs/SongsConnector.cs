using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Client.Songs.Helpers;
using KaraW3B.Server.Songs.Models.Helpers;
using KaraW3B.Server.Songs.Models.Songs;
using KaraW3B.Server.Songs.Models.Songs.Alerts;
using KaraW3B.Server.Songs.Models.Songs.Notes;

namespace KaraW3B.Client.Songs.Connectors.Songs
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

        public async Task<Song> GetSongAsync(Guid songId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get song details: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<Song>(responseStream
                , JsonHelper.DefaultJsonSerializerOptions, cancellationToken);
        }

        public async IAsyncEnumerable<SongNote> GetSongNotesAsync(Guid songId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}/notes"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get song details: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var note in JsonSerializer.DeserializeAsyncEnumerable<SongNote>(responseStream
                         , JsonHelper.DefaultJsonSerializerOptions, cancellationToken))
            {
                yield return note;
            }
        }

        public async IAsyncEnumerable<SongAlert> GetSongAlertsAsync(Guid songId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}/alerts"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get song details: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var alert in JsonSerializer.DeserializeAsyncEnumerable<SongAlert>(responseStream
                               , JsonHelper.DefaultJsonSerializerOptions, cancellationToken))
            {
                yield return alert;
            }
        }

        public async Task<Stream> GetSongFileStreamAsync(Guid songId, FileType fileType,
            CancellationToken cancellationToken)
        {
            var response =
                await _httpClient.GetAsync(_baseUri.AppendPath($"{songId}/streams/{fileType}"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KaraW3BSongsClientException(
                    $"Unable to get song file stream {fileType}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}