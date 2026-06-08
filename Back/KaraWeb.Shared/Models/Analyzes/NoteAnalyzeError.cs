using KaraWeb.Shared.Models.Songs.Notes;

namespace KaraWeb.Shared.Models.Analyzes
{
    public sealed class NoteAnalyzeError
    {
        public NoteAnalyzeError(string message, IAnalyzableSongNote note = null)
        {
            FileLine = note?.FileLine;
            Message = message;
        }

        public int? FileLine { get; }

        public string Message { get; }
    }
}
