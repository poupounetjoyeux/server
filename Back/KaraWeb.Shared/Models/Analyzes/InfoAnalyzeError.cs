namespace KaraWeb.Shared.Models.Analyzes
{
    public sealed class InfoAnalyzeError
    {
        public InfoAnalyzeError(string message, bool isWarning = false)
        {
            Message = message;
            IsWarning = isWarning;
        }

        public bool IsWarning { get; }

        public string Message { get; }
    }
}
