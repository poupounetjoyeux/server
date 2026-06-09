using System;
using System.Linq;
using System.Threading.Tasks;
using KaraWeb.SDK.Connectors;
using KaraWeb.Shared.Models.Songs.Files;
using NUnit.Framework;

namespace KaraWeb.Tests.SDK
{
    [TestFixture]
    public sealed class SongsConnectorTest
    {
        [Test, Explicit("Test SDK methods with a real API instance")]
        public async Task TestSDKInteration()
        {
            using var connector = new KaraWebConnector(new Uri("http://localhost:7373/api"));

            var library = await connector.Libraries.GetLibrariesAsync().FirstOrDefaultAsync();
            Assert.That(library, Is.Not.Null, "We should have load at least one library");

            var librarySong = await connector.Libraries.GetSongsAsync(library.Id, false).FirstOrDefaultAsync(s => s.HasAudio);
            Assert.That(librarySong, Is.Not.Null, $"The library with ID {library.Id} should contains at least one song with audio");

            var songDetails = await connector.Songs.GetSongDetailsAsync(librarySong.Id);
            Assert.That(songDetails, Is.Not.Null, $"We should be able to get song details for {librarySong.Title} - {librarySong.Artist}");

            Assert.That(songDetails.Id, Is.EqualTo(librarySong.Id), "Retrieved song details are not those of the same song..");

            await using var audioStream = await connector.Songs.GetSongFileStreamAsync(songDetails.Id, FileType.Audio);
            Assert.That(audioStream, Is.Not.Null, $"We should be able to get the audio stream for {librarySong.Title} - {librarySong.Artist}");
        }
    }
}
