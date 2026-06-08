namespace KaraWeb.Shared.Models.Songs.Messages
{
    /// <summary>
    ///     A message regarding song alerts
    /// </summary>
    public class SongAlertDto
    {
        /// <summary>
        ///     The type of the alert
        /// </summary>
        public AlertType Type { get; set; }

        /// <summary>
        ///     The message of the alert
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     The note line number in song file is error is related to a note
        /// </summary>
        public int? NoteFileLine { get; set; }
    }
}