namespace KaraW3B.Server.Songs.Core.Models.Settings
{
    public sealed class ConversionSettings
    {
        /// <summary>
        /// Flag to disable/enable the conversion
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// A threshold to know what we must convert
        /// </summary>
        public ConversionStatus Threshold { get; set; } = ConversionStatus.Mandatory;

        public ConversionSettings Clone()
        {
            return new ConversionSettings
            {
                Enabled = true,
                Threshold = Threshold
            };
        }
    }
}
