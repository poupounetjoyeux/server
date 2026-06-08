namespace KaraWeb.Shared.Models.Songs.Notes
{
    public interface IAnalyzableSongNote
    {
        int FileLine { get; }
        NoteType Type { get; }
        int PlayerNumber { get; }
        int StartBeat { get; }
        int Duration { get; }
        int? Pitch { get; }
        string Text { get; }
    }
}