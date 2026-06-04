using KaraWeb.Core.Persistence;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Libraries;
using log4net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
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
                await dbContext.Songs.Where(s => !parsedSongIds.Contains(s.Id)).ToListAsync(cancellationToken);
            dbContext.RemoveRange(songsToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);

            timeWatcher.Stop();
            _logger.Info(
                $"Library {library.Name} parsed {parsedSongIds.Count} songs and deleted {songsToDelete.Count} songs successfully in {timeWatcher.Elapsed}");
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
            _logger.Info($"Starting analyze of song file '{songFile.FullName}'");
            await using var dbContext = new KaraWebDbContext();
            var existingSong = await dbContext.Songs.Include(s => s.Notes).Include(s => s.Alerts).SingleOrDefaultAsync(
                s =>
                    s.SongFilePath == songFile.FullName &&
                    s.LibraryId == libraryId, cancellationToken);

            var fileHash = await ComputeFileHash(songFile, cancellationToken);

            Song song = null;
            if (analyzeType == LibraryAnalyzeType.Optimized)
            {
                if (existingSong != null && existingSong.AnalyzedFileHash == fileHash)
                {
                    _logger.Info($"Same hash already in DB for file '{songFile.FullName}'");
                    song = existingSong;
                }
            }

            song ??= await _songParserService.ParseSongAsync(libraryId, songFile, fileHash, cancellationToken);
            if (song == null)
            {
                return;
            }

            song.Alerts.RemoveAll(a =>
                a.Type is AlertType.MissingFile or AlertType.ValidationError or AlertType.ValidationWarning);

            var errorsResult = await SongHelper.CheckFullSong(song, song.Notes, cancellationToken);
            errorsResult.Errors.ForEach(e => song.AddAlert(AlertType.ValidationError, e));
            errorsResult.Warnings.ForEach(w => song.AddAlert(AlertType.ValidationWarning, w));

            await CheckSongFilesExistence(song, cancellationToken);

            if (existingSong != null)
            {
                song.Id = existingSong.Id;
                dbContext.Entry(existingSong).CurrentValues.SetValues(song);
            }
            else
            {
                await dbContext.Songs.AddAsync(song, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            parsedSongIds.Add(song.Id);
            _logger.Info($"Song file '{songFile.FullName}' metadata stored in DB");
        }

        private static Task CheckSongFilesExistence(Song song, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                CheckSongFilesExistence(song, song.Audio, FileType.Audio);
                CheckSongFilesExistence(song, song.Video, FileType.Video);
                CheckSongFilesExistence(song, song.Cover, FileType.Cover);
                CheckSongFilesExistence(song, song.Background, FileType.Background);
                CheckSongFilesExistence(song, song.Vocals, FileType.Vocals);
                CheckSongFilesExistence(song, song.Instrumental, FileType.Instrumental);
            }, cancellationToken);
        }

        private static void CheckSongFilesExistence(Song song, string fileValue, FileType fileType)
        {
            if (!string.IsNullOrEmpty(fileValue) && !song.SongFileExist(fileType))
            {
                song.AddAlert(AlertType.MissingFile, $"The {fileType} file '{fileValue}' doesn't exist on server");
            }
        }
    }
}