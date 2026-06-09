using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence.Songs;
using KaraWeb.Shared.Exceptions;
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
            new("^#(?<headerName>[A-Z0-9]+): *(?<headerValue>.*) *$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex PlayerRegex =
            new("^(DUETSINGER)?P *(?<playerNumber>[1-9])$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex NoteRegex =
            new(@"^(?<noteType>[:*RGF]) (?<startBeat>\d+) (?<duration>\d+) (?<pitch>-?\d+) (?<text>.*)$",
                RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex EolRegex =
            new(@"^-( (?<startBeat>\d+))?",
                RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly ILog _logger = LogManager.GetLogger(nameof(SongParserService));

        public async Task<bool> ParseSongAsync(FileInfo songFile, Song song,
            CancellationToken cancellationToken)
        {
            if (!songFile.Exists)
            {
                _logger.Error($"The song file '{songFile.FullName}' was not found");
                return false;
            }

            ResetSongInfos(song);

            _logger.Info($"Start parsing song file '{songFile.FullName}'");
            var timeWatch = new Stopwatch();
            timeWatch.Start();

            FileStream fileStream = null;
            StreamReader reader = null;
            SongNote lastParsedNote = null;
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
                var parsedHeaders = new HashSet<string>();
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                    {
                        break;
                    }

                    currentLine++;
                    if (string.IsNullOrEmpty(line.Trim()))
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

                        if (TryParseHeader(song, line, parsedHeaders, currentLine))
                        {
                            continue;
                        }
                    }

                    var newNote = TryParseEndOfPhraseNote(lastParsedNote, line, currentPlayer, currentLine) ??
                                  TryParseNote(line, currentPlayer, currentLine);
                    if (newNote != null)
                    {
                        lastParsedNote = newNote;
                        song.Notes.Add(newNote);
                        allHeadersParsed = true;
                        continue;
                    }

                    var playerMatch = PlayerRegex.Match(line);
                    if (playerMatch.Success)
                    {
                        currentPlayer = int.Parse(playerMatch.Groups["playerNumber"].Value, CultureInfo.InvariantCulture);
                        lastParsedNote = song.Notes.Where(n => n.PlayerNumber == currentPlayer).MaxBy(n => n.FileLine);
                        allHeadersParsed = true;
                        continue;
                    }

                    song.AddAlert(AlertType.ParsingError, "The line cannot be parsed", currentLine);
                }

                if (!eofMarkerFound)
                {
                    song.AddAlert(AlertType.ParsingWarning, "The song doesn't contains the 'E' EOF marker");
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
                reader.Dispose();
                if (fileStream != null)
                {
                    await fileStream.DisposeAsync();
                }
            }

            song.LastParseTime = DateTime.Now;
            return true;
        }

        private static void ResetSongInfos(Song song)
        {
            song.Version = null;
            song.Bpm = null;
            song.Title = null;
            song.Artist = null;
            song.Audio = null;
            song.Gap = null;
            song.Start = null;
            song.End = null;
            song.Players.Clear();

            song.Cover = null;
            song.Background = null;
            song.Video = null;
            song.VideoGap = null;
            song.Vocals = null;
            song.Instrumental = null;
            song.PreviewStart = null;
            song.MedleyStart = null;
            song.MedleyEnd = null;
            song.Year = null;

            song.Genres.Clear();
            song.Languages.Clear();
            song.Editions.Clear();
            song.Tags.Clear();

            song.Creator = null;
            song.ProvidedBy = null;
            song.Comment = null;
            song.AudioUrl = null;
            song.VideoUrl = null;
            song.CoverUrl = null;
            song.BackgroundUrl = null;
            song.Rendition = null;
            song.Encoding = null;
            song.NotManagedHeaders.Clear();

            song.Alerts.Clear();
            song.Notes.Clear();
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

        private static bool TryParseHeader(Song song, string fileLine, HashSet<string> parsedHeaders, int currentLine)
        {
            var headerLineMatch = HeaderRegex.Match(fileLine);
            if (!headerLineMatch.Success)
            {
                return false;
            }

            var headerName = headerLineMatch.Groups["headerName"].Value.ToUpperInvariant();
            var headerValue = headerLineMatch.Groups["headerValue"].Value;

            if (string.IsNullOrEmpty(headerValue))
            {
                song.AddAlert(AlertType.ParsingWarning, $"The header #{headerName} has no value and can be removed", currentLine);
                return true;
            }

            if (headerValue.Length > SongValidationHelper.MaxRecommendedHeaderSize)
            {
                song.AddAlert(AlertType.ParsingWarning, $"The header #{headerName} has a value greater than {SongValidationHelper.MaxRecommendedHeaderSize} bytes", currentLine);
            }

            if (HandleCoreHeaders(headerName, headerValue, song, currentLine, out var fixedHeaderName) ||
                HandleExtraHeaders(headerName, headerValue, song, currentLine, out fixedHeaderName))
            {
                if (!parsedHeaders.Add(fixedHeaderName))
                {
                    song.AddAlert(AlertType.ParsingWarning, $"The header #{headerName}is duplicated", currentLine);
                }

                return true;
            }

            if (HandlePlayerHeaders(headerName, headerValue, song, parsedHeaders, currentLine))
            {
                return true;
            }
            
            song.NotManagedHeaders.Add($"{headerName}={headerValue}");
            return true;
        }

        private static bool HandleCoreHeaders(string headerName, string headerValue, Song song, int currentLine, out string fixedHeaderName)
        {
            fixedHeaderName = headerName;
            switch (headerName)
            {
                case "VERSION":
                    song.Version = headerValue;
                    break;

                case "BPM":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var bpm))
                    {
                        song.Bpm = bpm;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be an decimal", currentLine);
                    }
                    break;

                case "MP3":
                case "AUDIO":
                    if (headerName == "MP3")
                    {
                        song.AddAlert(AlertType.ParsingWarning, "#MP3 header should be replaced by #AUDIO", currentLine);
                        fixedHeaderName = "AUDIO";
                    }

                    song.Audio = headerValue;
                    break;

                case "TITLE":
                    song.Title = headerValue;
                    break;

                case "ARTIST":
                    song.Artist = headerValue;
                    break;

                case "GAP":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var gap))
                    {
                        song.Gap = gap;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be seconds decimal or ms integer", currentLine);
                    }
                    break;

                case "START":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var start))
                    {
                        song.Start = start;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be seconds decimal or ms integer", currentLine);
                    }
                    break;

                case "END":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var end))
                    {
                        song.End = end;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be seconds decimal or ms integer", currentLine);
                    }
                    break;

                case "RELATIVE":
                    if (headerValue.Equals("YES", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new KaraWebException($"Relative mode is no more used in recent format versions.{Environment.NewLine}" +
                                                   $"Please avoid it since files order is less permissive and this program don't aim to support it.{Environment.NewLine}" +
                                                   $"You can always convert your old relative file by using UltraStar Deluxe song editor or UltraStar manager");
                    }

                    song.AddAlert(AlertType.ParsingWarning, "The #RELATIVE header is not enabled and can be removed", currentLine);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private static bool HandleExtraHeaders(string headerName, string headerValue, Song song, int currentLine, out string fixedHeaderName)
        {
            fixedHeaderName = headerName;
            switch (headerName)
            {
                case "COVER":
                    song.Cover = headerValue;
                    break;

                case "BACKGROUND":
                    song.Background = headerValue;
                    break;

                case "VIDEO":
                    song.Video = headerValue;
                    break;

                case "VOCALS":
                    song.Vocals = headerValue;
                    break;

                case "INSTRUMENTALS":
                case "INSTRUMENTAL":
                    song.Instrumental = headerValue;
                    if (headerName == "INSTRUMENTALS")
                    {
                        song.AddAlert(AlertType.ParsingWarning,
                            "#INSTRUMENTALS header doesn't takes a terminal 'S' in specifications", currentLine);
                        fixedHeaderName = "INSTRUMENTAL";
                    }
                    break;

                case "VIDEOGAP":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var videoGap))
                    {
                        song.VideoGap = videoGap;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be seconds decimal or ms integer", currentLine);
                    }
                    break;

                case "PREVIEW":
                case "PREVIEWSTART":
                    if (decimal.TryParse(headerValue, CultureInfo.InvariantCulture, out var previewStart))
                    {
                        song.PreviewStart = previewStart;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be seconds decimal or ms integer", currentLine);
                    }

                    if (headerName == "PREVIEW")
                    {
                        song.AddAlert(AlertType.ParsingWarning, "#PREVIEW header is deprecated and should be replaced by #PREVIEWSTART", currentLine);
                        fixedHeaderName = "PREVIEWSTART";
                    }
                    break;

                case "MEDLEYSTARTBEAT":
                case "MEDLEYSTART":

                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var medleyStart))
                    {
                        song.MedleyStart = medleyStart;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be an integer", currentLine);
                    }

                    if (headerName == "MEDLEYSTARTBEAT")
                    {
                        song.AddAlert(AlertType.ParsingWarning, 
                            "#MEDLEYSTARTBEAT header is deprecated and should be replaced by #MEDLEYSTART", currentLine);
                        fixedHeaderName = "MEDLEYSTART";
                    }
                    break;

                case "MEDLEYENDBEAT":
                case "MEDLEYEND":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var medleyEnd))
                    {
                        song.MedleyEnd = medleyEnd;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be an integer", currentLine);
                    }

                    if (headerName == "MEDLEYENDBEAT")
                    {
                        song.AddAlert(AlertType.ParsingWarning, "#MEDLEYENDBEAT header is deprecated and should be replaced by #MEDLEYEND", currentLine);
                        fixedHeaderName = "MEDLEYEND";
                    }
                    break;

                case "YEAR":
                    if (int.TryParse(headerValue, CultureInfo.InvariantCulture, out var year))
                    {
                        song.Year = year;
                    }
                    else
                    {
                        song.AddAlert(AlertType.ParsingError, $"Unable to parse #{headerName} header, it must be an integer", currentLine);
                    }
                    break;

                case "GENRE":
                    song.Genres.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    break;

                case "LANGUAGE":
                    song.Languages.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    break;

                case "EDITION":
                    song.Editions.AddRange(headerValue
                        .Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    break;

                case "TAGS":
                    song.Tags.AddRange(headerValue.Split(new[] { ListSplitter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()));
                    break;

                case "AUTHOR":
                case "CREATOR":
                    if (headerName == "AUTHOR")
                    {
                        song.AddAlert(AlertType.ParsingWarning, "#AUTHOR header is custom and should be replaced by #CREATOR", currentLine);
                        fixedHeaderName = "CREATOR";
                    }

                    song.Creator = headerValue;
                    break;

                case "PROVIDEDBY":
                    song.ProvidedBy = headerValue;
                    break;

                case "COMMENT":
                    song.Comment = headerValue;
                    break;

                case "AUDIOURL":
                    song.AudioUrl = headerValue;
                    break;

                case "VIDEOURL":
                    song.VideoUrl = headerValue;
                    break;

                case "COVERURL":
                    song.CoverUrl = headerValue;
                    break;

                case "BACKGROUNDURL":
                    song.BackgroundUrl = headerValue;
                    break;

                case "RENDITION":
                    song.Rendition = headerValue;
                    break;

                default:
                    return false;
            }

            return true;
        }

        private static bool HandlePlayerHeaders(string headerName, string headerValue, Song song, HashSet<string> parsedHeaders, int currentLine)
        {
            var playerHeaderMatch = PlayerRegex.Match(headerName);
            if (!playerHeaderMatch.Success)
            {
                return false;
            }

            var playerNumber = int.Parse(playerHeaderMatch.Groups["playerNumber"].Value, CultureInfo.InvariantCulture);

            if (headerName.StartsWith("DUETSINGER"))
            {
                if (parsedHeaders.Contains($"P{playerNumber}"))
                {
                    song.AddAlert(AlertType.ParsingWarning, $"#{headerName} header can be removed since #P{playerNumber} header is present", currentLine);
                    return true;
                }

                song.AddAlert(AlertType.ParsingWarning, $"#{headerName} header is deprecated and should be replaced by #P{playerNumber}", currentLine);
            }

            var player = song.Players.SingleOrDefault(p => p.Number == playerNumber);
            if (player == null)
            {
                player = new SongPlayer { Number = playerNumber };
                song.Players.Add(player);
            }

            player.Name = headerValue;
            if (!parsedHeaders.Add(headerName))
            {
                song.AddAlert(AlertType.ParsingWarning, $"#{headerName} header is duplicated", currentLine);
            }
            return true;
        }

        #endregion

        #region Notes

        private static SongNote TryParseEndOfPhraseNote(SongNote lastParsedNote, string fileLine, int playerNumber, int currentLine)
        {
            var eolMatch = EolRegex.Match(fileLine);
            if (!eolMatch.Success)
            {
                return null;
            }

            var note = new SongNote
            {
                FileLine = currentLine,
                PlayerNumber = playerNumber,
                Type = NoteType.EndOfPhrase,
                StartBeat = -1,
                Duration = null
            };

            if (eolMatch.Groups["startBeat"].Success)
            {
                note.StartBeat = int.Parse(eolMatch.Groups["startBeat"].Value, CultureInfo.InvariantCulture);
            } 
            else if (lastParsedNote != null)
            {
                var duration = 1;
                if (lastParsedNote.Duration.HasValue)
                {
                    duration = lastParsedNote.Duration.Value;
                }
                note.StartBeat = lastParsedNote.StartBeat + duration - 1;
            }
            return note;
        }

        private static SongNote TryParseNote(string fileLine, int playerNumber, int currentLine)
        {
            var noteMatch = NoteRegex.Match(fileLine);
            if (!noteMatch.Success)
            {
                return null;
            }

            return new SongNote
            {
                FileLine = currentLine,
                PlayerNumber = playerNumber,
                Type = ParseNoteType(noteMatch.Groups["noteType"].Value),
                StartBeat = int.Parse(noteMatch.Groups["startBeat"].Value, CultureInfo.InvariantCulture),
                Duration = int.Parse(noteMatch.Groups["duration"].Value, CultureInfo.InvariantCulture),
                Pitch = int.Parse(noteMatch.Groups["pitch"].Value, CultureInfo.InvariantCulture),
                Text = noteMatch.Groups["text"].Value
            };
        }

        private static NoteType ParseNoteType(string noteType)
        {
            return noteType.ToUpperInvariant() switch
            {
                ":" => NoteType.Regular,
                "*" => NoteType.Golden,
                "R" => NoteType.Rap,
                "G" => NoteType.GoldenRap,
                _ => NoteType.Freestyle
            };
        }

        #endregion
    }
}