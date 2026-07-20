namespace KaraW3B.Server.Songs.Models.Libraries
{
    /// <summary>
    ///     The payload to set analyze options
    /// </summary>
    public class LibraryAnalyzePayload
    {
        /// <summary>
        ///     The type of analyze you want to start
        /// </summary>
        public LibraryAnalyzeType AnalyzeType { get; set; }
    }
}