using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Shared.Models.Libraries;

namespace KaraWeb.Core.Services.LibrariesAnalyzer
{
    public interface ILibrariesAnalyzerService
    {
        Task StartLibraryAnalyzeAsync(ILibrary library, LibraryAnalyzeType analyzeType,
            CancellationToken cancellationToken);
    }
}