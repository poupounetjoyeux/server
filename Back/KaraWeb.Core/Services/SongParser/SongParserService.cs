using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Songs.Notes;
using log4net;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.Core.Services.SongParser
{
    /// <summary>
    ///     Song parsing is based on https://github.com/UltraStar-Deluxe/format and https://usdx.eu/format/
    ///     It aims to be able to handle the maximum of files wherever they are up to date or not
    /// </summary>
    public sealed class SongParserService : ISongParserService
    {
        private const char ListSplitter = ',';
        private const string EofMarker = "E";

        private static readonly Regex EncodingRegex = new("^#ENCODING: *(?<encoding>.+) *$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex HeaderRegex =
            new("^#(?<headerName>[A-Z0-9]+): *(?<headerValue>.+) *$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex PlayerRegex =
            new("^(DUETSINGER)?P(?<playerNumber>[1-9])$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex NoteRegex =
            new(@"^(?<noteType>[:*RGF-]) (?<startBeat>\d+)( (?<duration>\d+) (?<pitch>-?\d+) (?<text>.*))?$",
                RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly ILog _logger = LogManager.GetLogger(nameof(SongParserService));

        public async Task<Song> ParseSongAsync(Guid libraryId, FileInfo songFile, string fileHash,
            CancellationToken cancellationToken)
        {
            if (!songFile.Exists)
            {
                _logger.Error($"The song file '{songFile.FullName}' was not found");
                return null;
            }

            _logger.Info($"Start parsing song file '{songFile.FullName}'");
            var timeWatch = new Stopwatch();
            timeWatch.Start();

            var fileLines =
                await File.ReadAllLinesAsync(songFile.FullName, EncodingHelper.GetDefaultEncoding(), cancellationToken);
            var song = new Song
            {
                LibraryId = libraryId,
                SongFilePath = songFile.FullName,
                AnalyzedFileHash = fileHash
            };

            try
            {
                // Reload with the specified encoding
                if (HasEncodingSpecified(song, fileLines) && !EncodingHelper.IsDefaultEncoding(song.Encoding))
                {
                    fileLines = await File.ReadAllLinesAsync(songFile.FullName,
                        EncodingHelper.GetEncoding(song.Encoding), cancellationToken);
                }

                await Parallel.ForEachAsync(fileLines, cancellationToken, (l, c) => ParseHeaders(song, l, c));
                
                if (song.NotManagedHeaders.Count > 0)
                {
                    song.Warnings.Add($"There is {song.NotManagedHeaders.Count} unmanaged headers that should be removed");
                }

                var errorsResult = await SongHelper.CheckHeadersErrorsAsync(song, cancellationToken);
                song.Errors.AddRange(errorsResult.Errors);
                song.Warnings.AddRange(errorsResult.Warnings);

                await ParseNotes(song, fileLines, cancellationToken);

                errorsResult = await SongHelper.CheckNotesErrorsAsync(song, song.Notes, cancellationToken);
                song.Errors.AddRange(errorsResult.Errors);
                song.Warnings.AddRange(errorsResult.Warnings);

                timeWatch.Stop();
                _logger.Info($"Song file '{songFile.FullName}' successfully parsed in {timeWatch.Elapsed}");
            }
            catch (Exception e)
            {
                timeWatch.Stop();
                _logger.Error($"There was an error when parsing song file '{songFile.FullName}': {e}");
                song.Errors.Add($"The song cannot be parsed: {e}");
            }

            return song;
        }

        private static bool HasEncodingSpecified(Song song, string[] fileLines)
        {
            foreach (var fileLine in fileLines)
            {
                var declaredEncoding = EncodingRegex.Match(fileLine);
                if (!declaredEncoding.Success)
                {
                    continue;
                }

                song.Encoding = EncodingHelper.SanitizeEncodingName(declaredEncoding.Groups["encoding"].Value);
                return true;
            }

            return false;
        }

        private static bool IsEofMarker(string line)
        {
            return line.Trim().Equals(EofMarker, StringComparison.InvariantCultureIgnoreCase);
        }

        #region Headers

        private ValueTask ParseHeaders(Song song, string fileLine, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var headerLineMatch = HeaderRegex.Match(fileLine);
            if (!headerLineMatch.Success)
            {
                return ValueTask.CompletedTask;
            }

            var headerName = headerLineMatch.Groups["headerName"].Value.ToUpperInvariant();
            var headerValue = headerLineMatch.Groups["headerValue"].Value;

            cancellationToken.ThrowIfCancellationRequested();
            if (HandleCoreHeaders(headerName, headerValue, song))
            {
                return ValueTask.CompletedTask;
            }


            cancellationToken.ThrowIfCancellationRequested();
            if (HandleExtraHeaders(headerName, headerValue, song))
            {
                return ValueTask.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (HandlePlayerHeaders(headerName, headerValue, song))
            {
                return ValueTask.CompletedTask;
            }

            song.NotManagedHeaders.Add($"{headerName}={headerValue}");
            return ValueTask.CompletedTask;
        }

        private bool HandleCoreHeaders(string headerName, string headerValue, Song song)
        {
            switch (headerName)
            {
                case "VERSION":
                    song.Version = headerValue;
                    return true;

                case "BPM":
                    if (double.TryParse(headerValue, CultureInfo.InvariantCulture, out var bpm))
                    {
                        song.Bpm = bpm;
                    }

                    return true;

                case "MP3":
                case "AUDIO":
                    if (headerName == "MP3")
                    {
                        song.Warnings.Add("#MP3 header should be replaced by #AUDIO");
                    }

                    song.Audio = headerValue;
                    return true;

                case "TITLE":
                    song.Title = headerValue;
                    return true;

                case "ARTIST":
                    song.Artist = headerValue;
                    return true;

                case "GAP":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var gap))
                    {
                        song.Gap = gap;
                    }

                    return true;

                case "START":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var start))
                    {
                        song.Start = start;
                    }

                    return true;

                case "END":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var end))
                    {
                        song.End = end;
                    }

                    return true;

                default:
                    return false;
            }
        }

        private bool HandleExtraHeaders(string headerName, string headerValue, Song song)
        {
            switch (headerName)
            {
                case "COVER":
                    song.Cover = headerValue;
                    return true;

                case "BACKGROUND":
                    song.Background = headerValue;
                    return true;

                case "VIDEO":
                    song.Video = headerValue;
                    return true;

                case "VOCALS":
                    song.Vocals = headerValue;
                    return true;

                case "INSTRUMENTAL":
                    song.Instrumental = headerValue;
                    return true;

                case "VIDEOGAP":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var videoGap))
                    {
                        song.VideoGap = videoGap;
                    }

                    return true;

                case "PREVIEW":
                case "PREVIEWSTART":
                    if (headerName == "PREVIEW")
                    {
                        song.Warnings.Add("#PREVIEW header is deprecated and should be replaced by #PREVIEWSTART");
                    }

                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var previewStart))
                    {
                        song.PreviewStart = previewStart;
                    }

                    return true;

                case "MEDLEYSTARTBEAT":
                case "MEDLEYSTART":
                    if (headerName == "MEDLEYSTARTBEAT")
                    {
                        song.Warnings.Add(
                            "#MEDLEYSTARTBEAT header is deprecated and should be replaced by #MEDLEYSTART");
                    }

                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var medleyStart))
                    {
                        song.MedleyStart = medleyStart;
                    }

                    return true;

                case "MEDLEYENDBEAT":
                case "MEDLEYEND":
                    if (headerName == "MEDLEYENDBEAT")
                    {
                        song.Warnings.Add("#MEDLEYENDBEAT header is deprecated and should be replaced by #MEDLEYEND");
                    }

                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var medleyEnd))
                    {
                        song.MedleyEnd = medleyEnd;
                    }

                    return true;

                case "YEAR":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var year))
                    {
                        song.Year = year;
                    }

                    return true;

                case "GENRE":
                    song.Genres.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    return true;

                case "LANGUAGE":
                    song.Languages.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    return true;

                case "EDITION":
                    song.Editions.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    return true;

                case "TAGS":
                    song.Tags.AddRange(headerValue.Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    return true;

                case "AUTHOR":
                case "CREATOR":
                    if (headerName == "AUTHOR")
                    {
                        song.Warnings.Add("#AUTHOR header is deprecated and should be replaced by #CREATOR");
                    }

                    song.Creator = headerValue;
                    return true;

                case "PROVIDEDBY":
                    song.ProvidedBy = headerValue;
                    return true;

                case "COMMENT":
                    song.Comment = headerValue;
                    return true;

                case "AUDIOURL":
                    song.AudioUrl = headerValue;
                    return true;

                case "VIDEOURL":
                    song.VideoUrl = headerValue;
                    return true;

                case "COVERURL":
                    song.CoverUrl = headerValue;
                    return true;

                case "BACKGROUNDURL":
                    song.BackgroundUrl = headerValue;
                    return true;

                case "RENDITION":
                    song.Rendition = headerValue;
                    return true;

                default:
                    return false;
            }
        }

        private bool HandlePlayerHeaders(string headerName, string headerValue, Song song)
        {
            var playerHeaderMatch = PlayerRegex.Match(headerName);
            if (!playerHeaderMatch.Success)
            {
                return false;
            }

            if (headerName.StartsWith("DUETSINGER"))
            {
                song.Warnings.Add($"#{headerName} header is deprecated and should be replaced by #P1 to #P9");
            }

            if (!int.TryParse(playerHeaderMatch.Groups["playerNumber"].Value, out var playerNumber))
            {
                return false;
            }

            song.Players.Add(new SongPlayer { SongId = song.Id, Number = playerNumber, Name = headerValue });
            return true;
        }

        #endregion

        #region Notes

        private Task ParseNotes(Song song, string[] fileLines, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                song.Notes.Clear();
                var currentPlayer = 1;
                var hasEofMarker = false;
                foreach (var fileLine in fileLines)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsEofMarker(fileLine))
                    {
                        hasEofMarker = true;
                        break;
                    }

                    var noteMatch = NoteRegex.Match(fileLine);
                    if (!noteMatch.Success)
                    {
                        var playerMatch = PlayerRegex.Match(fileLine);
                        if (playerMatch.Success && int.TryParse(playerMatch.Groups["playerNumber"].Value,
                                CultureInfo.InvariantCulture,
                                out var playerNumber))
                        {
                            currentPlayer = playerNumber;
                        }

                        continue;
                    }

                    var note = new SongNote
                    {
                        PlayerNumber = currentPlayer,
                        SongId = song.Id,
                        Type = ParseNoteType(noteMatch.Groups["noteType"].Value),
                        StartBeat = int.Parse(noteMatch.Groups["startBeat"].Value, CultureInfo.InvariantCulture)
                    };

                    if (note.Type != NoteType.Eol)
                    {
                        note.Duration = int.TryParse(noteMatch.Groups["duration"].Value, CultureInfo.InvariantCulture,
                            out var duration)
                            ? duration
                            : null;
                        note.Pitch = int.TryParse(noteMatch.Groups["pitch"].Value, CultureInfo.InvariantCulture,
                            out var pitch)
                            ? pitch
                            : null;
                        note.Text = noteMatch.Groups["text"].Value;
                    }

                    song.Notes.Add(note);
                }

                if (!hasEofMarker)
                {
                    song.Errors.Add("The song doesn't contains the 'E' EOF marker");
                }
            }, cancellationToken);
        }

        private static NoteType ParseNoteType(string noteType)
        {
            return noteType.ToUpperInvariant() switch
            {
                "-" => NoteType.Eol,
                ":" => NoteType.Regular,
                "*" => NoteType.Golden,
                "R" => NoteType.Rap,
                "G" => NoteType.GoldenRap,
                "F" => NoteType.Freestyle,
                _ => NoteType.Unknow
            };
        }

        #endregion
    }
}