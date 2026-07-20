using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Core.Models;
using KaraW3B.Server.Songs.Models.Songs;

namespace KaraW3B.Server.Songs.Core.Services.FFmpeg
{
    public interface IFFmpegService
    {
        Task<ConversionStatus> GetVideoCompatibilityAsync(string videoPath, CancellationToken cancellationToken);
        Task<ConversionStatus> GetAudioCompatibilityAsync(string audioPath, CancellationToken cancellationToken);
        Task EnqueueTranscodeAsync(Song song, FileType fileType, CancellationToken cancellationToken);
    }
}
