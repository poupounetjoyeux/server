using System.Collections.Generic;

namespace KaraWeb.Shared.Models.Analyzes
{
    public sealed class FullAnalyzeResult
    {
        public List<HeaderAnalyzeError> HeadersErrors { get; } = new();
        public List<NoteAnalyzeError> NotesErrors { get; } = new();
    }
}