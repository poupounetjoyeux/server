using System.Collections.Generic;

namespace KaraWeb.Shared.Models.Songs
{
    public interface IAnalyzableSong
    {
        string Encoding { get; }
        string Version { get; }
        string Title { get; }
        string Artist { get; }
        string Audio { get; }
        string Video { get; }
        string Cover { get; }
        string Background { get; }
        double? Bpm { get; }
        string AudioUrl { get; }
        string VideoUrl { get; }
        string CoverUrl { get; }
        string BackgroundUrl { get; }
        List<string> Languages { get; }
        List<string> Genres { get; }
        Dictionary<int, string> GetPlayers();
    }
}