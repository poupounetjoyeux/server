using KaraWeb.Shared.Models.Analyzes;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KaraWeb.Shared.Helpers
{
    /// <summary>
    ///     Song error checkers are based on https://github.com/UltraStar-Deluxe/format and https://usdx.eu/format/
    ///     They aim to be able to not reject the maximum of files wherever they are up to date or not
    ///     The goal is also to add recommendations to be compatible with the more recent format
    /// </summary>
    public static class SongValidationHelper
    {
        public const int MaxRecommendedHeaderSize = 2048;

        public static async Task<FullAnalyzeResult> CheckFullSongErrorsAsync(IFileHelper fileHelper, IAnalyzableSong song,
            IEnumerable<IAnalyzableSongNote> notes, CancellationToken cancellationToken)
        {
            var result = new FullAnalyzeResult();
            result.HeadersErrors.AddRange(await CheckHeadersErrorsAsync(fileHelper, song, cancellationToken));
            result.NotesErrors.AddRange(await CheckNotesErrorsAsync(song, notes, cancellationToken));
            return result;
        }

        #region Headers

        public static Task<List<HeaderAnalyzeError>> CheckHeadersErrorsAsync(IFileHelper fileHelper, IAnalyzableSong song,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var errors = new List<HeaderAnalyzeError>();

                if (CheckEncodingHeader(song.Encoding) is { } encodingError)
                {
                    errors.Add(encodingError);
                }

                if (CheckVersionHeader(song.Version) is { } versionError)
                {
                    errors.Add(versionError);
                }

                errors.AddRange(CheckMandatoryHeaders(song));
                errors.AddRange(CheckPathHeaders(fileHelper, song));

                if (CheckBpmHeader(song.Bpm) is { } bpmError)
                {
                    errors.Add(bpmError);
                }

                errors.AddRange(CheckMedleyHeaders(song.MedleyStart, song.MedleyEnd));
                errors.AddRange(CheckPlayerHeaders(song));
                errors.AddRange(CheckTimeHeaders(song));
                errors.AddRange(CheckUriHeaders(song));
                errors.AddRange(CheckLanguageHeader(song.Languages));

                return errors;
            }, cancellationToken);
        }

        public static HeaderAnalyzeError CheckEncodingHeader(string encoding)
        {
            if (string.IsNullOrEmpty(encoding))
            {
                return null;
            }

            var message = !EncodingHelper.IsDefaultEncoding(encoding)
                ? "Prefer using UTF8 (without BOM) for your files encoding"
                : "When song encoding is already in UTF8, #ENCODING header should be removed";
            return new HeaderAnalyzeError(message, true);

        }

        public static HeaderAnalyzeError CheckVersionHeader(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new HeaderAnalyzeError("It's recommended to add the #VERSION header at the top of your file", true);
            }

            var versionParts = version.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (versionParts.Length != 3 || versionParts.Any(p => !int.TryParse(p, out _)))
            {
                return new HeaderAnalyzeError("The #VERSION header format is incorrect must be X.Y.Z");
            }

            return null;
        }

        public static HeaderAnalyzeError CheckBpmHeader(decimal? bpm)
        {
            return bpm switch
            {
                null => new HeaderAnalyzeError("The song BPM is mandatory (#BPM header)"),
                <= 0 => new HeaderAnalyzeError("The song BPM must be grater than 0"),
                _ => null
            };
        }

        public static IEnumerable<HeaderAnalyzeError> CheckMedleyHeaders(int? medleyStart, int? medleyEnd)
        {
            if (medleyStart is < 0)
            {
                yield return new HeaderAnalyzeError("#MEDLEYSTART header must be positive");
            }

            if (medleyEnd is < 0)
            {
                yield return new HeaderAnalyzeError("#MEDLEYEND header must be positive");
            }

            if (medleyStart > medleyEnd)
            {
                yield return new HeaderAnalyzeError("#MEDLEYEND header must be greater than #MEDLEYSTART");
            }
        }

        public static IEnumerable<HeaderAnalyzeError> CheckMandatoryHeaders(IAnalyzableSong song)
        {
            var mandatoryHeadersToCheck = new Dictionary<string, string>
            {
                { "AUDIO", song.Audio },
                { "TITLE", song.Title },
                { "ARTIST", song.Artist }
            };

            foreach (var mandatoryHeader in mandatoryHeadersToCheck)
            {
                var error = CheckMandatoryHeader(mandatoryHeader.Key, mandatoryHeader.Value);
                if (error != null)
                {
                    yield return error;
                }
            }

        }

        public static HeaderAnalyzeError CheckMandatoryHeader(string headerName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new HeaderAnalyzeError($"The header #{headerName.ToUpperInvariant()} is mandatory");
            }

            return null;
        }

        public static IEnumerable<HeaderAnalyzeError> CheckPathHeaders(IFileHelper fileHelper, IAnalyzableSong song)
        {
            var pathHeadersToCheck = new Dictionary<string, string>
            {
                { "AUDIO", song.Audio },
                { "VIDEO", song.Video },
                { "COVER", song.Cover },
                { "BACKGROUND", song.Background },
                { "VOCALS", song.Vocals },
                { "INSTRUMENTAL", song.Instrumental },
            };

            foreach (var pathHeader in pathHeadersToCheck)
            {
                var error = CheckPathHeader(fileHelper, pathHeader.Key, pathHeader.Value);
                if (error != null)
                {
                    yield return error;
                }
            }
        }

        public static HeaderAnalyzeError CheckPathHeader(IFileHelper fileHelper, string headerName, string path)
        {
            if (!string.IsNullOrEmpty(path) && !fileHelper.IsRelativePath(path))
            {
                return new HeaderAnalyzeError(
                    $"The #{headerName.ToUpperInvariant()} header should be a relative path");
            }

            return null;
        }

        public static List<HeaderAnalyzeError> CheckLanguageHeader(IEnumerable<string> languages)
        {
            return languages.Where(l => !LanguagesHelper.IsValidLanguage(l)).Select(l =>
                new HeaderAnalyzeError($"The language '{l}' is not an ISO 639.2 english name language", true)).ToList();
        }

        public static IEnumerable<HeaderAnalyzeError> CheckPlayerHeaders(IAnalyzableSong song)
        {
            var songPlayers = song.GetPlayers();
            foreach (var songPlayer in songPlayers)
            {
                if (string.IsNullOrEmpty(songPlayer.Value))
                {
                    yield return new HeaderAnalyzeError(
                        $"The player {songPlayer.Key} has no name defined (#P{songPlayer.Key} header)");
                }

                if (songPlayer.Key > songPlayers.Count)
                {
                    yield return new HeaderAnalyzeError(
                        $"Prefer using player indexes by ascending order. #P{songPlayer.Key} could be replaced by a lower player number value",
                        true);
                }
            }
        }

        public static IEnumerable<HeaderAnalyzeError> CheckUriHeaders(IAnalyzableSong song)
        {
            var uriHeadersToCheck = new Dictionary<string, string>
            {
                { "AUDIOURL", song.AudioUrl },
                { "VIDEOURL", song.VideoUrl },
                { "COVERURL", song.CoverUrl },
                { "BACKGROUNDURL", song.BackgroundUrl },
            };

            foreach (var uriHeader in uriHeadersToCheck)
            {
                var error = CheckUriHeader(uriHeader.Key, uriHeader.Value);
                if (error != null)
                {
                    yield return error;
                }
            }
        }

        public static HeaderAnalyzeError CheckUriHeader(string headerName, string uri)
        {
            if (!string.IsNullOrEmpty(uri) && !Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                return new HeaderAnalyzeError(
                    $"The #{headerName.ToUpperInvariant()} header should be a valid URL according to RFC 1738", true);
            }

            return null;
        }

        public static IEnumerable<HeaderAnalyzeError> CheckTimeHeaders(IAnalyzableSong song)
        {
            var timeHeadersToCheck = new Dictionary<string, (decimal?, bool)>
            {
                { "GAP", (song.Gap, true) },
                { "START", (song.Start, true) },
                { "END", (song.End, true) },
                { "VIDEOGAP", (song.VideoGap, false) },
                { "PREVIEWSTART", (song.PreviewStart, true) },
            };

            foreach (var timeHeader in timeHeadersToCheck)
            {
                foreach (var error in CheckTimeHeader(timeHeader.Key, timeHeader.Value.Item1, timeHeader.Value.Item2))
                {
                    yield return error;
                }
            }
        }

        public static IEnumerable<HeaderAnalyzeError> CheckTimeHeader(string headerName, decimal? time, bool mustBePositive)
        {
            var errors = new List<HeaderAnalyzeError>();
            if (!time.HasValue)
            {
                return errors;
            }

            if (time % 1 != 0)
            {
                errors.Add(new HeaderAnalyzeError($"The new format version recommend a #{headerName.ToUpperInvariant()} header in ms (integer)", true));
            }

            if (mustBePositive && time < 0)
            {
                errors.Add(new HeaderAnalyzeError($"#{headerName.ToUpperInvariant()} header value must be positive"));
            }

            return errors;
        }

#endregion

        public static Task<List<NoteAnalyzeError>> CheckNotesErrorsAsync(IAnalyzableSong song,
            IEnumerable<IAnalyzableSongNote> songNotes,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var errors = new List<NoteAnalyzeError>();
                var processedPlayers = new HashSet<int>();

                foreach (var playerNotes in songNotes.GroupBy(n => n.PlayerNumber))
                {
                    processedPlayers.Add(playerNotes.Key);
                    var orderedPlayerNotes = playerNotes.OrderBy(n => n.StartBeat).ToArray();

                    for (var i = 0; i < orderedPlayerNotes.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var analyzedNote = orderedPlayerNotes[i];

                        if (analyzedNote.StartBeat < 0)
                        {
                            errors.Add(new NoteAnalyzeError("The note cannot have a negative start beat", analyzedNote));
                        }

                        if (analyzedNote.Type == NoteType.EndOfPhrase)
                        {
                            if (i == 0)
                            {
                                errors.Add(new NoteAnalyzeError("The first note must never be an end of phrase", analyzedNote));
                                continue;
                            }

                            if (orderedPlayerNotes[i - 1].Type == NoteType.EndOfPhrase)
                            {
                                errors.Add(new NoteAnalyzeError("There is subsequent end of phrase markers", analyzedNote));
                            }

                            if (i == orderedPlayerNotes.Length - 1)
                            {
                                errors.Add(new NoteAnalyzeError("The last note must never be an end of phrase", analyzedNote));
                            }
                        }
                        else
                        {
                            if (!analyzedNote.Pitch.HasValue)
                            {
                                errors.Add(new NoteAnalyzeError("The pitch is mandatory", analyzedNote));
                            }

                            if (string.IsNullOrEmpty(analyzedNote.Text))
                            {
                                errors.Add(new NoteAnalyzeError("A text is mandatory for note", analyzedNote));
                            }

                            if (analyzedNote.Duration < 1)
                            {
                                errors.Add(new NoteAnalyzeError("Duration is less than 1", analyzedNote));
                            }
                        }

                        if (i == 0)
                        {
                            continue;
                        }

                        var previousNote = orderedPlayerNotes[i - 1];
                        var previousEndBeat = previousNote.StartBeat;
                        if (previousNote.Duration.HasValue)
                        {
                            previousEndBeat += previousNote.Duration.Value;
                        }

                        if (analyzedNote.StartBeat < previousEndBeat)
                        {
                            errors.Add(new NoteAnalyzeError("There is an overlap with the previous note", analyzedNote));
                        }
                    }
                }

                if (processedPlayers.Count == 0)
                {
                    errors.Add(new NoteAnalyzeError("There is no note defined"));
                    return errors;
                }

                // No player info or only standard player one
                var songPlayers = song.GetPlayers();
                if (processedPlayers.Count == 1 && songPlayers.Count == 0)
                {
                    return errors;
                }

                foreach (var playerNumber in processedPlayers.Where(playerNumber => !songPlayers.ContainsKey(playerNumber)))
                {
                    errors.Add(new NoteAnalyzeError(
                        $"There is notes for player {playerNumber} defined but no corresponding player name using #P{playerNumber} header"));
                }

                foreach (var songPlayer in songPlayers.Where(songPlayer => !processedPlayers.Contains(songPlayer.Key)))
                {
                    errors.Add(new NoteAnalyzeError(
                        $"There is no notes defined for player {songPlayer.Key} that is defined with the #P{songPlayer.Key} header"));
                }

                return errors;
            }, cancellationToken);
        }
    }
}