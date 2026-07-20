using System;
using System.Linq;
using System.Threading.Tasks;
using KaraW3B.Client.Songs.Connectors;
using KaraW3B.Server.Songs.Models.Songs;
using NUnit.Framework;

namespace KaraW3B.Server.Songs.Tests.Connectors
{
    [TestFixture]
    public sealed class SongsConnectorTest
    {
        [Test, Explicit("Test SDK methods with a real API instance")]
        public async Task TestSDKInteraction()
        {
            using var connector = new KaraW3BConnector(new Uri("http://localhost:7373/api"));

            var library = await connector.Libraries.GetLibrariesAsync().FirstOrDefaultAsync();
            Assert.That(library, Is.Not.Null, "We should have load at least one library");

            var librarySong = await connector.Libraries.GetSongsAsync(library.Id, false)
                .FirstOrDefaultAsync(s => !string.IsNullOrEmpty(s.Audio));
            Assert.That(librarySong, Is.Not.Null,
                $"The library with ID {library.Id} should contains at least one song with audio");

            var song = await connector.Songs.GetSongAsync(librarySong.Id);
            Assert.That(song, Is.Not.Null,
                $"We should be able to get song details for {librarySong.Title} - {librarySong.Artist}");

            Assert.That(song.Id, Is.EqualTo(librarySong.Id),
                "Retrieved song details are not those of the same song..");

            var notes = await connector.Songs.GetSongNotesAsync(song.Id).ToListAsync();
            Assert.That(notes, Is.Not.Empty, "There was no note retrieved");

            await using var audioStream = await connector.Songs.GetSongFileStreamAsync(song.Id, FileType.Audio);
            Assert.That(audioStream, Is.Not.Null,
                $"We should be able to get the audio stream for {librarySong.Title} - {librarySong.Artist}");
        }
    }
}