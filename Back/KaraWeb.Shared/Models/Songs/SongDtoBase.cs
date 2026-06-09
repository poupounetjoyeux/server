using KaraWeb.Shared.Models.Songs.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KaraWeb.Shared.Models.Songs
{
    /// <summary>
    ///     Base properties of a song parsed from file
    /// </summary>
    public abstract class SongDtoBase
    {
        /// <summary>
        ///     The song's ID
        /// </summary>
        public Guid Id { get; set; }

        #region Core headers

        /// <summary>
        ///     The UltraStar format version used
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     The BPM of the song
        /// </summary>
        public decimal? Bpm { get; set; }

        /// <summary>
        ///     The song's title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     The song's artist
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        ///     The GAP between start of the audio and first beat
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public decimal? Gap { get; set; }

        /// <summary>
        ///     The GAP between start of the audio and first beat
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public decimal? Start { get; set; }

        /// <summary>
        ///     The GAP between start of the audio and first beat
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public decimal? End { get; set; }

        /// <summary>
        ///     Song's players defined from #P1 to #P9
        /// </summary>
        public List<SongPlayerDto> Players { get; set; } = new();

        public Dictionary<int, string> GetPlayers()
        {
            return Players.ToDictionary(p => p.PlayerNumber, p => p.Name);
        }

        #endregion

        #region Extra headers

        /// <summary>
        ///     The video delay relative to the audio file
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public decimal? VideoGap { get; set; }

        /// <summary>
        ///     The offset of audio file to use for preview
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public decimal? PreviewStart { get; set; }

        /// <summary>
        ///     The offset of audio file to use to start in a medley
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public int? MedleyStart { get; set; }

        /// <summary>
        ///     The offset of audio file to use to finish in a medley
        /// </summary>
        /// <remarks>milliseconds</remarks>
        public int? MedleyEnd { get; set; }

        /// <summary>
        ///     The song's year
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        ///     Song's genres
        /// </summary>
        public List<string> Genres { get; set; } = new();

        /// <summary>
        ///     The song's languages
        /// </summary>
        /// <remarks>Using ISO 639-2 english languages</remarks>
        public List<string> Languages { get; set; } = new();

        /// <summary>
        ///     Song's editions
        /// </summary>
        public List<string> Editions { get; set; } = new();

        /// <summary>
        ///     Song's tags
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        ///     The song's creator
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        ///     The source of the song
        /// </summary>
        /// <remarks>valid http(s) URL according to RFC 1738</remarks>
        public string ProvidedBy { get; set; }

        /// <summary>
        ///     Comment about the song
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        ///     Specific recording of the song
        /// </summary>
        public string Rendition { get; set; }

        /// <summary>
        ///     Specific encoding of the song file
        /// </summary>
        public string Encoding { get; set; }

        #endregion
    }
}