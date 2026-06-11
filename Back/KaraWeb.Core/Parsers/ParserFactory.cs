using KaraWeb.Core.Persistence.Models.Songs;
using KaraWeb.Shared.Exceptions;

namespace KaraWeb.Core.Parsers
{
    internal static class ParserFactory
    {
        public static ParserBase GetParser(this Song song)
        {
            if (song.Version == null)
            {
                return new UnversionedFormatParser(song);
            }

            if (song.Version.Major == 1)
            {
                return new V1FormatParser(song);
            }

            if (song.Version.Major == 2)
            {
                return new V2FormatParser(song);
            }

            throw new KaraWebException($"$The version {song.Version.ToString(3)} has no parser implementation");
        }
    }
}
