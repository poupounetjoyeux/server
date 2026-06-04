namespace KaraWeb.Shared.Models.Songs.Notes
{
    public interface ISongNote
    {
        NoteType Type { get; }
        int PlayerNumber { get; }
        int StartBeat { get; }
        int? Duration { get; }
        int? Pitch { get; }
        string Text { get; }
    }
}