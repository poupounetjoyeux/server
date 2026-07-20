namespace KaraW3B.Server.Songs.Models.Songs.Alerts
{
    /// <summary>
    ///     A message regarding song alerts
    /// </summary>
    public class SongAlert
    {
        /// <summary>
        ///     The type of the alert
        /// </summary>
        public AlertType Type { get; set; }

        /// <summary>
        /// The level of the alert
        /// </summary>
        public AlertLevel Level { get; set; }

        /// <summary>
        ///     The message of the alert
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     The note line number in song file is error is related to a note
        /// </summary>
        public int? FileLine { get; set; }
    }
}