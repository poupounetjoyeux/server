using System;
using System.Collections.Generic;
using KaraWeb.Shared.Models.Songs.Medleys;

namespace KaraWeb.Shared.Models.Songs
{
    public interface IAnalyzableSong
    {
        Version Version { get; }
        string Title { get; }
        string Artist { get; }
        string Audio { get; }
        decimal Bpm { get; }
        TimeSpan? Gap { get; }
        TimeSpan? Start { get; }
        TimeSpan? End { get; }
        string Video { get; }
        TimeSpan? VideoGap { get; }
        string Vocals { get; }
        string Instrumental { get; }
        TimeSpan? PreviewStart { get; }
        string Cover { get; }
        string Background { get; }
        string AudioUrl { get; }
        string VideoUrl { get; }
        string CoverUrl { get; }
        string BackgroundUrl { get; }
        List<string> Languages { get; }
        Dictionary<int, string> GetPlayers();
        ISongMedley GetMedley();
    }
}