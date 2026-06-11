using KaraWeb.Core.Helper;
using KaraWeb.Core.Parsers;
using KaraWeb.Core.Persistence.Models.Songs;
using KaraWeb.Shared;
using KaraWeb.Shared.Exceptions;
using KaraWeb.Shared.Helpers;
using KaraWeb.Shared.Models.Songs.Messages;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly IFileHelper _fileHelper;

        private const string EofMarker = "E";

        private static readonly Regex EncodingRegex = new("^#ENCODING: *(?<encoding>.+) *$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex VersionRegex = new("^#VERSION: *(?<version>.+) *$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly ILog _logger = LogManager.GetLogger(nameof(SongParserService));

        public SongParserService(IFileHelper fileHelper)
        {
            _fileHelper = fileHelper;
        }

        public Task<bool> ParseSongAsync(FileInfo songFile, Song song,
            CancellationToken cancellationToken)
        {
            return ParseSongInternalAsync(songFile, song, new ParsingOptions(), cancellationToken);
        }

        private async Task<bool> ParseSongInternalAsync(FileInfo songFile, Song song, ParsingOptions options, 
            CancellationToken cancellationToken)
        {
            if (!songFile.Exists)
            {
                _logger.Error($"The song file '{songFile.FullName}' was not found");
                return false;
            }

            ResetSongInfos(song, options.Version);

            _logger.Info($"Start parsing song file '{songFile.FullName}'");
            var timeWatch = new Stopwatch();
            timeWatch.Start();

            StreamReader reader = null;
            var line = 0;
            try
            {
                var fileStream = new FileStream(
                    songFile.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                reader = new StreamReader(fileStream, options.Encoding);

                var parser = song.GetParser();

                var eofMarkerFound = false;
                var allHeadersParsed = false;
                var parsedHeadersCount = 0;
                var mandatoryHeadersChecked = false;
                while (true)
                {
                    var fileLine = await reader.ReadLineAsync(cancellationToken);
                    if (fileLine == null)
                    {
                        break;
                    }

                    line++;
                    if (string.IsNullOrEmpty(fileLine.Trim()))
                    {
                        continue;
                    }

                    if (IsEofMarker(fileLine))
                    {
                        eofMarkerFound = true;
                        break;
                    }

                    if (!allHeadersParsed)
                    {
                        if (options.EncodingHeaderLine != line && TryParseSpecificEncoding(options.Encoding, song, fileLine, line, out var newEncoding))
                        {
                            if (parsedHeadersCount > 0)
                            {
                                song.AddParsingWarning(
                                    "The #ENCODING header must be on top of the song file to improve loading performances");
                            }

                            if (newEncoding != null)
                            {
                                reader.Dispose();
                                reader = null;
                                return await ParseSongInternalAsync(songFile, song, options.WithEncoding(newEncoding, line),
                                    cancellationToken);
                            }
                        }

                        if (options.VersionHeaderLine != line && TryParseSpecificVersion(song, fileLine))
                        {
                            if (parsedHeadersCount > 0)
                            {
                                song.AddParsingWarning(
                                    "The #VERSION header must be on top of the song file to improve loading performances");
                            }

                            if (song.Version != null)
                            {
                                reader.Dispose();
                                reader = null;
                                return await ParseSongInternalAsync(songFile, song, options.WithVersion(song.Version, line),
                                    cancellationToken);
                            }
                        }

                        if (parser.TryParseFileHeaderLine(fileLine, line))
                        {
                            parsedHeadersCount++;
                            continue;
                        }
                    }

                    if (!mandatoryHeadersChecked && !parser.AreMandatoryHeadersDefined())
                    {
                        break;
                    }

                    mandatoryHeadersChecked = true;
                    if (parser.TryParseFileNoteLine(fileLine, line))
                    {
                        allHeadersParsed = true;
                        continue;
                    }

                    song.AddParsingError("The line cannot be parsed", line);
                }

                if (!eofMarkerFound)
                {
                    song.AddParsingWarning("The song doesn't contains the 'E' EOF marker");
                }

                parser.PostParsing();

                var analyzeResult = await SongValidationHelper.CheckFullSongErrorsAsync(_fileHelper, song, song.Notes, cancellationToken);
                song.AddAnalyzeAlerts(analyzeResult);

                timeWatch.Stop();
                _logger.Info(
                    $"Song file '{songFile.FullName}' successfully parsed in {timeWatch.Elapsed} with {song.Alerts.Count(a => a.Level == AlertLevel.Error)} error(s) and {song.Alerts.Count(a => a.Level == AlertLevel.Warning)} warning(s)");
            }
            catch (KaraWebException e)
            {
                song.AddParsingError(e.Message, line);
            }
            catch (Exception e)
            {
                song.AddParsingError($"There is an exception when parsing the song file: {e}", line);
            }
            finally
            {
                timeWatch.Stop();
                reader?.Dispose();
            }

            song.LastParseTime = DateTime.Now;
            return true;
        }

        private static void ResetSongInfos(Song song, Version version)
        {
            song.Version = version;
            song.Bpm = -1;
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
            song.Medley = null;
            song.Year = null;

            song.Genres.Clear();
            song.Languages.Clear();
            song.Editions.Clear();
            song.Tags.Clear();
            song.Creators.Clear();

            song.ProvidedBy = null;
            song.Comment = null;
            song.AudioUrl = null;
            song.VideoUrl = null;
            song.CoverUrl = null;
            song.BackgroundUrl = null;
            song.Rendition = null;
            song.NotManagedHeaders.Clear();

            song.Alerts.Clear();
            song.Notes.Clear();
        }

        private static bool TryParseSpecificEncoding(Encoding currentEncoding, Song song, string fileLine, int line, out Encoding encoding)
        {
            encoding = null;
            var declaredEncoding = EncodingRegex.Match(fileLine);
            if (!declaredEncoding.Success)
            {
                return false;
            }

            if (currentEncoding != null)
            {
                throw new KaraWebException("The #ENCODING header is duplicated");
            }

            var sanitizedEncoding = EncodingHelper.SanitizeEncodingName(declaredEncoding.Groups["encoding"].Value);
            if (EncodingHelper.IsDefaultEncoding(sanitizedEncoding))
            {
                song.AddParsingWarning("The #ENCODING header is deprecated. Your song is already in UTF-8 (which is recommended), you can just remove it!", line);
            }
            else
            {
                song.AddParsingWarning(
                    "The #ENCODING header is deprecated. All file should be in UTF-8 (without BOM)", line);
                encoding = EncodingHelper.GetEncoding(sanitizedEncoding);
            }

            return true;
        }

        private static bool TryParseSpecificVersion(Song song, string fileLine)
        {
            var declaredVersion = VersionRegex.Match(fileLine);
            if (!declaredVersion.Success)
            {
                return false;
            }

            if (song.Version != null)
            {
                throw new KaraWebException("The #VERSION header is duplicated");
            }

            if (!Version.TryParse(declaredVersion.Groups["version"].Value, out var version))
            {
                throw new KaraWebException("The #VERSION header cannot be parsed. Format must be X.Y.Z");
            }

            song.Version = version;
            return true;
        }

        private static bool IsEofMarker(string line)
        {
            return line.Trim().Equals(EofMarker, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}