using System;

namespace KaraW3B.Server.Songs.Models.Songs
{
    /// <summary>
    ///     The song medley information
    /// </summary>
    public class SongMedley
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