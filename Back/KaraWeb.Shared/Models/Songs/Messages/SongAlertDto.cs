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
    }
}