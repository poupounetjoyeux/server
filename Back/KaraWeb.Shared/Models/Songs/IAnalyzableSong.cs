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
        decimal? Gap { get; }
        decimal? Start { get; }
        decimal? End { get; }
        string Video { get; }
        decimal? VideoGap { get; }
        string Vocals { get; }
        string Instrumental { get; }
        decimal? PreviewStart { get; }
        string Cover { get; }
        string Background { get; }
        decimal? Bpm { get; }
        int? MedleyStart { get; }
        int? MedleyEnd { get; }
        string AudioUrl { get; }
        string VideoUrl { get; }
        string CoverUrl { get; }
        string BackgroundUrl { get; }
        List<string> Languages { get; }
        Dictionary<int, string> GetPlayers();
    }
}