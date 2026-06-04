namespace KaraWeb.Shared.Models.Songs
{
    /// <summary>
    ///     A simple and light song parsed from file
    /// </summary>
    public sealed class SongDto : SongDtoBase
    {

        #region Core headers

        /// <summary>
        ///     A flag that indicates if the song has the audio file
        /// </summary>
        public bool HasAudio { get; set; }

        #endregion

        #region Extra headers

        /// <summary>
        ///     A flag that indicates if the song has the cover file
        /// </summary>
        public bool HasCover { get; set; }

        /// <summary>
        ///     A flag that indicates if the song has the background file
        /// </summary>
        public bool HasBackground { get; set; }

        /// <summary>
        ///     A flag that indicates if the song has the video file
        /// </summary>
        public bool HasVideo { get; set; }

        /// <summary>
        ///     A flag that indicates if the song has the instrumental file
        /// </summary>
        public bool HasVocals { get; set; }

        /// <summary>
        ///     A flag that indicates if the song has the vocal file
        /// </summary>
        public bool HasInstrumental { get; set; }

        #endregion

        #region Internal

        /// <summary>
        ///     A flag that indicates if the song contains errors
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        ///     A flag that indicates if the song contains warnings
        /// </summary>
        public bool HasWarnings { get; set; }

        #endregion
    }
}
