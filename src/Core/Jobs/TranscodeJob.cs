using System.Threading.Tasks;
using KaraW3B.Server.Songs.Core.Helpers;
using KaraW3B.Server.Songs.Core.Persistence.Models.Songs;
using KaraW3B.Server.Songs.Core.Services.FFmpeg;
using KaraW3B.Server.Songs.Models.Songs;
using log4net;
using Quartz;

namespace KaraW3B.Server.Songs.Core.Jobs
{
    internal sealed class TranscodeJob : IJob
    {
        public static readonly JobKey JobKey = new(nameof(TranscodeJob), KaraW3BConstants.ApplicationName);
        private readonly ILog _logger = LogManager.GetLogger(JobKey.Name);

        public const string SongKey = "song";
        public const string FileTypeKey = "file_type";
        public const string FFmpegServiceKey = "FFmpeg_service";

        public async Task Execute(IJobExecutionContext context)
        {
            if (context.MergedJobDataMap[SongKey] is not DbSong song)
            {
                _logger.Error("Unable to retrieve a valid song from job context");
                return;
            }

            if (context.MergedJobDataMap[FileTypeKey] is not FileType fileType)
            {
                _logger.Error("Unable to retrieve a valid file type from job context");
                return;
            }

            if (context.MergedJobDataMap[FFmpegServiceKey] is not IFFmpegService ffmpegService)
            {
                _logger.Error("Unable to retrieve a valid FFmpeg service from job context");
                return;
            }

            //TODO implement the logic
        }
    }
}
