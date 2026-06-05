using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Libraries;
using log4net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Shared.Models.Songs.Files;
using KaraWeb.Shared.Models.Songs.Messages;

namespace KaraWeb.Core.Services.LibrariesAnalyzer
{
    public sealed class LibrariesAnalyzerService : ILibrariesAnalyzerService
    {
        private readonly ILog _logger = LogManager.GetLogger(nameof(LibrariesAnalyzerService));
        private readonly ISongParserService _songParserService;

        public LibrariesAnalyzerService(ISongParserService songParserService)
        {
            _songParserService = songParserService;
        }

        public async Task StartLibraryAnalyzeAsync(IAnalyzableLibrary library, LibraryAnalyzeType analyzeType,
            CancellationToken cancellationToken)
        {
            // TODO: Make it background
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
            await Parallel.ForEachAsync(foundFiles, cancellationToken,
                (f, c) => ProcessSongFile(library.Id, analyzeType, parsedSongIds, f, c));

            await using var dbContext = new KaraWebDbContext();
            var songsToDelete =
                await dbContext.Songs.Where(s => s.LibraryId == library.Id && !parsedSongIds.Contains(s.Id)).ToListAsync(cancellationToken);
            if (songsToDelete.Count > 0)
            {
                dbContext.RemoveRange(songsToDelete);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            timeWatcher.Stop();
            _logger.Info(
                $"Library {library.Name} analyzed {parsedSongIds.Count} song(s) and deleted {songsToDelete.Count} song(s) successfully in {timeWatcher.Elapsed}");
        }

        private static async Task<string> ComputeFileHash(FileInfo file, CancellationToken cancellationToken)
        {
            var fileBytes = await File.ReadAllBytesAsync(file.FullName, cancellationToken);
            var hashBytes = SHA1.HashData(fileBytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        private async ValueTask ProcessSongFile(Guid libraryId, LibraryAnalyzeType analyzeType,
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

                    if (!await _songParserService.ParseSongAsync(songFile, song, cancellationToken))
                    {
                        _logger.Error($"Unable to parse data from '{songFile.FullName}'.. Ignoring it..");
                        return;
                    }

                    if (isNew)
                    {
                        await dbContext.Songs.AddAsync(song, cancellationToken);
                    }
                }
                else
                {
                    foreach (var songAlert in song.Alerts.Where(a => a.Type != AlertType.ParsingError && a.Type != AlertType.ParsingWarning).ToList())
                    {
                        song.Alerts.Remove(songAlert);
                    }
                }

                _logger.Info($"Checking errors on song '{songFile.FullName}'");

                var errorsResult = await SongHelper.CheckFullSong(song, song.Notes, cancellationToken);
                errorsResult.Errors.ForEach(e => song.AddAlert(AlertType.ValidationError, e));
                errorsResult.Warnings.ForEach(w => song.AddAlert(AlertType.ValidationWarning, w));

                var missingFilesErrors = await CheckSongFilesExistence(song, cancellationToken);
                missingFilesErrors.ForEach(m => song.AddAlert(AlertType.MissingFile, m));

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