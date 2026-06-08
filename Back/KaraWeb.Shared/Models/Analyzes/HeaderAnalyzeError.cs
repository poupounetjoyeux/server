namespace KaraWeb.Shared.Models.Analyzes
{
    public sealed class HeaderAnalyzeError
    {
        public HeaderAnalyzeError(string message, bool isWarning = false)
        {
            Message = message;
            IsWarning = isWarning;
        }

        public bool IsWarning { get; }

        public string Message { get; }
    }
}
