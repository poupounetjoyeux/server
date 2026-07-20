using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using KaraW3B.Server.Songs.Core.Helpers;
using KaraW3B.Server.Songs.Core.Models;
using KaraW3B.Server.Songs.Core.Services.Settings;
using Quartz;
using System.Threading;
using System.Threading.Tasks;
using KaraW3B.Server.Songs.Core.Jobs;
using KaraW3B.Server.Songs.Core.Services.Scheduler;
using KaraW3B.Server.Songs.Models.Songs;

namespace KaraW3B.Server.Songs.Core.Services.FFmpeg
{
    public sealed class FFmpegService : IFFmpegService
    {
        private const string EncodedByTag = "encoded_by";

        private readonly ApiScheduler _scheduler;

        public FFmpegService(ISettingsService settingsService, ISchedulerService schedulerService)
        {
            var customFFmpegPath = settingsService.Settings.FFmpegPath;
            if (!string.IsNullOrEmpty(customFFmpegPath))
            {
                GlobalFFOptions.Configure(options => options.BinaryFolder = customFFmpegPath);
            }

            _scheduler = schedulerService.RegisterSchedulerAsync("Transcoder", settingsService.Settings.ConcurrencySettings.MaxConversionConcurrency, CancellationToken.None).Result;
            _scheduler.RegisterJobAsync<TranscodeJob>(TranscodeJob.JobKey, CancellationToken.None).Wait();
        }

        public async Task<ConversionStatus> GetVideoCompatibilityAsync(string videoPath, CancellationToken cancellationToken)
        {
            var mediaInfos = await FFProbe.AnalyseAsync(videoPath, cancellationToken: cancellationToken);

            if ((mediaInfos.Format.Tags?.TryGetValue(EncodedByTag, out var value) ?? false) &&
                value == KaraW3BConstants.ApplicationName)
            {
                return ConversionStatus.Compatible;
            }

            if (!mediaInfos.Format.FormatName.Contains("mp4"))
            {
                return ConversionStatus.Mandatory;
            }

            var videoStream = mediaInfos.PrimaryVideoStream;
            if (videoStream == null)
            {
                return ConversionStatus.Mandatory;
            }

            if (videoStream.PixelFormat != "yuv420p")
            {
                return ConversionStatus.Recommended;
            }

            if (videoStream.CodecName != VideoCodec.LibX264.Name)
            {
                return ConversionStatus.Recommended;
            }

            return ConversionStatus.Compatible;
        }

        public async Task<ConversionStatus> GetAudioCompatibilityAsync(string audioPath, CancellationToken cancellationToken)
        {
            var mediaInfos = await FFProbe.AnalyseAsync(audioPath, cancellationToken: cancellationToken);

            if ((mediaInfos.Format.Tags?.TryGetValue(EncodedByTag, out var value) ?? false) &&
                value == KaraW3BConstants.ApplicationName)
            {
                return ConversionStatus.Compatible;
            }

            if (mediaInfos.Format.FormatName != "mp3")
            {
                return ConversionStatus.Mandatory;
            }

            var audioStream = mediaInfos.PrimaryAudioStream;
            if (audioStream == null)
            {
                return ConversionStatus.Mandatory;
            }

            if (audioStream.CodecName != "mp3")
            {
                return ConversionStatus.Recommended;
            }

            return ConversionStatus.Compatible;
        }

        public async Task EnqueueTranscodeAsync(Song song, FileType fileType, CancellationToken cancellationToken)
        {
            var dataMap = new JobDataMap
            {
                [TranscodeJob.SongKey] = song,
                [TranscodeJob.FileTypeKey] = fileType,
                [TranscodeJob.FFmpegServiceKey] = this
            };
            await _scheduler.StartJob(AnalyzeLibraryJob.JobKey, dataMap, cancellationToken);
        }

        private class MovFlagsArgument : IArgument
        {
            private readonly string _flag;

            public MovFlagsArgument(string flag)
            {
                _flag = flag;
            }

            public string Text => $"-movflags {_flag}";
        }

        private class KaraW3BConvertedMetadataArgument : IArgument
        {
            public string Text => $"-metadata {EncodedByTag}=\"{KaraW3BConstants.ApplicationName}\"";
        }
    }
}
