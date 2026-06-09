using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Libraries;
using KaraWeb.Shared.Models.Songs.Files;
using log4net;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Libraries;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Shared;
using KaraWeb.Shared.Models.Songs.Messages;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Jobs
{
    public sealed class AnalyzeLibraryJob : IJob
    {
        public static readonly JobKey JobKey = new(nameof(AnalyzeLibraryJob), KaraWebConstants.Name);
        private readonly ILog _logger = LogManager.GetLogger(JobKey.Name);

        public const string LibraryKey = "library";
        public const string AnalyzeTypeKey = "analyze_type";
        public const string SongParserServiceKey = "song_parser_service";
        public const string FileHelperKey = "file_helper";

        public async Task Execute(IJobExecutionContext context)
        {
            if (context.MergedJobDataMap[LibraryKey] is not Library library)
            {
                _logger.Error("Unable to retrieve a valid library from job context");
                return;
            }

            if (context.MergedJobDataMap[AnalyzeTypeKey] is not LibraryAnalyzeType analyzeType)
            {
                _logger.Error("Unable to retrieve a valid analyze type from job context");
                return;
            }

            if (context.MergedJobDataMap[SongParserServiceKey] is not ISongParserService songParserService)
            {
                _logger.Error("Unable to retrieve a valid song parser service from job context");
                return;
            }

            if (context.MergedJobDataMap[FileHelperKey] is not IFileHelper fileHelper)
            {
                _logger.Error("Unable to retrieve a valid file helper from job context");
                return;
            }

            await using var dbContext = new KaraWebDbContext();
            library = dbContext.Attach(library).Entity;
            if (library.IsAnalyzing)
            {
                _logger.Warn($"Library with ID {library.Id} is already analyzing.. Aborting..");
                return;
            }
            library.IsAnalyzing = true;
            library.LastAnalyzeMessage = null;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            var directory = new DirectoryInfo(library.Path);
            if (!directory.Exists)
            {
                _logger.Error($"Directory '{directory.FullName}' doesn't exist");
                return;
            }

            _logger.Info($"Start analyzing library '{library.Name}' in mode '{analyzeType}'");
            var timeWatcher = new Stopwatch();
            timeWatcher.Start();

            var foundFiles = directory.GetFiles("*.txt", SearchOption.AllDirectories);
            _logger.Info($"Found {foundFiles.Length} potential song file(s) to analyze");

            var parsedSongIds = new ConcurrentBag<Guid>();
            await Parallel.ForEachAsync(foundFiles, context.CancellationToken,
                (f, c) => ProcessSongFile(fileHelper, songParserService, library.Id, analyzeType, parsedSongIds, f, c));

            var songsToDelete =
                await dbContext.Songs.Where(s => s.LibraryId == library.Id && !parsedSongIds.Contains(s.Id))
                    .ToListAsync(context.CancellationToken);
            if (songsToDelete.Count > 0)
            {
                dbContext.RemoveRange(songsToDelete);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }

            timeWatcher.Stop();

            library.IsAnalyzing = false;
            library.LastAnalyzeMessage =
                $"Library {library.Name} analyzed {parsedSongIds.Count} song(s) and deleted {songsToDelete.Count} song(s) successfully in {timeWatcher.Elapsed}";
            await dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.Info(library.LastAnalyzeMessage);
        }

        private static async Task<string> ComputeFileHash(FileInfo file, CancellationToken cancellationToken)
        {
            var fileBytes = await File.ReadAllBytesAsync(file.FullName, cancellationToken);
            var hashBytes = SHA1.HashData(fileBytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        private async ValueTask ProcessSongFile(IFileHelper fileHelper, ISongParserService songParserService, Guid libraryId, LibraryAnalyzeType analyzeType,
            ConcurrentBag<Guid> parsedSongIds, FileInfo songFile, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info($"Starting analyze of song file '{songFile.FullName}'");
                await using var dbContext = new KaraWebDbContext();
                var song = await dbContext.Songs
                    .SingleOrDefaultAsync(
                        s =>
                            s.SongFilePath == songFile.FullName &&
                            s.LibraryId == libraryId, cancellationToken);

                var fileHash = await ComputeFileHash(songFile, cancellationToken);

                var needParsing = true;
                if (analyzeType == LibraryAnalyzeType.Optimized)
                {
                    if (song != null && song.AnalyzedFileHash == fileHash)
                    {
                        _logger.Info($"Same hash already in DB for file '{songFile.FullName}'");
                        needParsing = false;
                    }
                }

                if (needParsing)
                {
                    var isNew = false;
                    if (song == null)
                    {
                        song = new Song
                        {
                            LibraryId = libraryId,
                            SongFilePath = songFile.FullName,
                            AnalyzedFileHash = fileHash
                        };
                        isNew = true;
                    }

                    if (!await songParserService.ParseSongAsync(songFile, song, cancellationToken))
                    {
                        _logger.Error($"Unable to parse data from '{songFile.FullName}'.. Ignoring it..");
                        return;
                    }

                    if (isNew)
                    {
                        await dbContext.Songs.AddAsync(song, cancellationToken);
                    }

                    _logger.Info($"Checking errors on song '{songFile.FullName}'");

                    var analyzeResult = await SongValidationHelper.CheckFullSongErrorsAsync(fileHelper, song, song.Notes, cancellationToken);
                    analyzeResult.HeadersErrors.ForEach(e => song.AddAlert(e.IsWarning ? AlertType.HeaderWarning : AlertType.HeaderError, e.Message));
                    analyzeResult.NotesErrors.ForEach(e => song.AddAlert(AlertType.NoteError, e.Message, e.FileLine));
                }
                else
                {
                    foreach (var songAlert in song.Alerts.Where(a => a.Type == AlertType.MissingFileError).ToList())
                    {
                        song.Alerts.Remove(songAlert);
                    }
                }

                var missingFilesErrors = await CheckSongFilesExistence(song, cancellationToken);
                missingFilesErrors.ForEach(m => song.AddAlert(AlertType.MissingFileError, m));

                await dbContext.SaveChangesAsync(cancellationToken);
                parsedSongIds.Add(song.Id);
                _logger.Info($"Song file '{songFile.FullName}' metadata stored in DB");
            }
            catch (Exception e)
            {
                _logger.Error($"There was an error during {songFile.FullName} analyze: {e}");
            }
        }

        private static Task<List<string>> CheckSongFilesExistence(Song song, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var errors = new List<string>();
                CheckSongFilesExistence(errors, song, song.Audio, FileType.Audio);
                CheckSongFilesExistence(errors, song, song.Video, FileType.Video);
                CheckSongFilesExistence(errors, song, song.Cover, FileType.Cover);
                CheckSongFilesExistence(errors, song, song.Background, FileType.Background);
                CheckSongFilesExistence(errors, song, song.Vocals, FileType.Vocals);
                CheckSongFilesExistence(errors, song, song.Instrumental, FileType.Instrumental);
                return errors;
            }, cancellationToken);
        }

        private static void CheckSongFilesExistence(List<string> errors, Song song, string fileValue, FileType fileType)
        {
            if (!string.IsNullOrEmpty(fileValue) && !song.SongFileExist(fileType))
            {
                errors.Add($"The {fileType} file '{fileValue}' doesn't exist on server");
            }
        }
    }
}
