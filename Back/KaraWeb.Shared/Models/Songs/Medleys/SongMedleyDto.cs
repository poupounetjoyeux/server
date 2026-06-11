using System;

namespace KaraWeb.Shared.Models.Songs.Medleys
{
    /// <summary>
    ///     The song medley information
    /// </summary>
    public class SongMedleyDto : ISongMedley
    {
        /// <summary>
        ///     The time in the song when the medley should start
        /// </summary>
        public TimeSpan MedleyStart { get; set; }

        /// <summary>
        ///     The time in the song when the medley should start
        /// </summary>
        public TimeSpan MedleyEnd { get; set; }
    }
}
