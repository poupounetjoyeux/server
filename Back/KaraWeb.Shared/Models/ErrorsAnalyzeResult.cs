using System.Collections.Generic;

namespace KaraWeb.Shared.Models
{
    public sealed class ErrorsAnalyzeResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }
}
