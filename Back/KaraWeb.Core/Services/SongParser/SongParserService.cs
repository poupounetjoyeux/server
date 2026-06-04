using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Songs.Messages;
using KaraWeb.Shared.Models.Songs.Notes;
using log4net;

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

            var song = new Song
            {
                LibraryId = libraryId,
                SongFilePath = songFile.FullName,
                AnalyzedFileHash = fileHash
            };

            FileStream fileStream = null;
            StreamReader reader = null;

            try
            {
                fileStream = new FileStream(
                    songFile.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                reader = new StreamReader(fileStream, EncodingHelper.GetDefaultEncoding());

                var eofMarkerFound = false;
                var allHeadersParsed = false;
                var currentPlayer = 1;
                var currentLine = 0;
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                    {
                        break;
                    }

                    currentLine++;
                    if (string.IsNullOrEmpty(line?.Trim()))
                    {
                        continue;
                    }

                    if (IsEofMarker(line))
                    {
                        eofMarkerFound = true;
                        break;
                    }

                    if (!allHeadersParsed)
                    {
                        if (string.IsNullOrEmpty(song.Encoding) && TryParseSpecificEncoding(song, line))
                        {
                            reader.Dispose();
                            fileStream.Seek(0, SeekOrigin.Begin);
                            reader = new StreamReader(fileStream, EncodingHelper.GetEncoding(song.Encoding));
                            continue;
                        }

                        if (TryParseHeader(song, line))
                        {
                            continue;
                        }
                    }

                    allHeadersParsed = true;

                    if (TryParseNote(song, line, currentPlayer))
                    {
                        continue;
                    }

                    var playerMatch = PlayerRegex.Match(line);
                    if (playerMatch.Success && int.TryParse(playerMatch.Groups["playerNumber"].Value,
                            CultureInfo.InvariantCulture,
                            out var playerNumber))
                    {
                        currentPlayer = playerNumber;
                        continue;
                    }

                    song.AddAlert(AlertType.ParsingError, $"The line {currentLine} doesn't respect the required format");
                }

                if (!eofMarkerFound)
                {
                    song.AddAlert(AlertType.ParsingError, "The song doesn't contains the 'E' EOF marker");
                }

                timeWatch.Stop();
                _logger.Info($"Song file '{songFile.FullName}' successfully parsed in {timeWatch.Elapsed}");
            }
            catch (Exception e)
            {
                timeWatch.Stop();
                _logger.Error($"There was an error when parsing song file '{songFile.FullName}': {e}");
                song.AddAlert(AlertType.ParsingError, $"The song cannot be parsed: {e}");
            }
            finally
            {
                reader?.Dispose();
                if (fileStream != null)
                {
                    await fileStream.DisposeAsync();
                }
            }
            return song;
        }

        private static bool TryParseSpecificEncoding(Song song, string fileLine)
        {
            var declaredEncoding = EncodingRegex.Match(fileLine);
            if (!declaredEncoding.Success)
            {
                return false;
            }

            song.Encoding = EncodingHelper.SanitizeEncodingName(declaredEncoding.Groups["encoding"].Value);
            return true;
        }

        private static bool IsEofMarker(string line)
        {
            return line.Trim().Equals(EofMarker, StringComparison.InvariantCultureIgnoreCase);
        }

        #region Headers

        private bool TryParseHeader(Song song, string fileLine)
        {
            var headerLineMatch = HeaderRegex.Match(fileLine);
            if (!headerLineMatch.Success)
            {
                return false;
            }

            var headerName = headerLineMatch.Groups["headerName"].Value.ToUpperInvariant();
            var headerValue = headerLineMatch.Groups["headerValue"].Value;

            if (HandleCoreHeaders(headerName, headerValue, song))
            {
                return true;
            }
            
            if (HandleExtraHeaders(headerName, headerValue, song))
            {
                return true;
            }

            if (HandlePlayerHeaders(headerName, headerValue, song))
            {
                return true;
            }

            song.NotManagedHeaders.Add($"{headerName}={headerValue}");
            return true;
        }

        private static bool HandleCoreHeaders(string headerName, string headerValue, Song song)
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
                        song.AddAlert(AlertType.ParsingWarning, "#MP3 header should be replaced by #AUDIO");
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

        private static bool HandleExtraHeaders(string headerName, string headerValue, Song song)
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
                        song.AddAlert(AlertType.ParsingWarning, "#PREVIEW header is deprecated and should be replaced by #PREVIEWSTART");
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
                        song.AddAlert(AlertType.ParsingWarning, 
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
                        song.AddAlert(AlertType.ParsingWarning, "#MEDLEYENDBEAT header is deprecated and should be replaced by #MEDLEYEND");
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
                        song.AddAlert(AlertType.ParsingWarning, "#AUTHOR header is deprecated and should be replaced by #CREATOR");
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

        private static bool HandlePlayerHeaders(string headerName, string headerValue, Song song)
        {
            var playerHeaderMatch = PlayerRegex.Match(headerName);
            if (!playerHeaderMatch.Success)
            {
                return false;
            }

            if (headerName.StartsWith("DUETSINGER"))
            {
                song.AddAlert(AlertType.ParsingWarning, $"#{headerName} header is deprecated and should be replaced by #P1 to #P9");
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

        private bool TryParseNote(Song song, string fileLine, int playerNumber)
        {
            var noteMatch = NoteRegex.Match(fileLine);
            if (!noteMatch.Success)
            {
                return false;
            }

            var note = new SongNote
            {
                PlayerNumber = playerNumber,
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
            return true;
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