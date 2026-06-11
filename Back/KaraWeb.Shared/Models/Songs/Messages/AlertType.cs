namespace KaraWeb.Shared.Models.Songs.Messages
{
    /// <summary>
    /// The type of a song alert
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// There is an error during the parsing of the file
        /// </summary>
        Parsing,
        /// <summary>
        /// There is an error on song info
        /// </summary>
        Info,
        /// <summary>
        /// There is an error on notes
        /// </summary>
        Note,
        /// <summary>
        /// Some files related to the song are missing on disk
        /// </summary>
        MissingFile
    }
}