using KaraWeb.Shared.Models.Analyzes;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Notes;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static async Task<FullAnalyzeResult> CheckFullSong(IAnalyzableSong song,
            IEnumerable<IAnalyzableSongNote> notes, CancellationToken cancellationToken)
        {
            return new FullAnalyzeResult
            {
                HeadersErrors = await CheckHeadersErrorsAsync(song, cancellationToken),
                NotesErrors = await CheckNotesErrorsAsync(song, notes, cancellationToken)
            };
        }

        public static Task<List<HeaderAnalyzeError>> CheckHeadersErrorsAsync(IAnalyzableSong song,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var errors = new List<HeaderAnalyzeError>();
                if (!string.IsNullOrEmpty(song.Encoding))
                {
                    var message = !EncodingHelper.IsDefaultEncoding(song.Encoding)
                        ? "Prefer using UTF8 (without BOM) for your files encoding"
                        : "When song encoding is already in UTF8, #ENCODING header should be removed";
                    errors.Add(new HeaderAnalyzeError(message, true));
                }

                if (!string.IsNullOrEmpty(song.Version) && !Version.TryParse(song.Version, out _))
                {
                    errors.Add(new HeaderAnalyzeError("The version format is incorrect", true));
                }

                if (string.IsNullOrEmpty(song.Title))
                {
                    errors.Add(new HeaderAnalyzeError("A title is mandatory (#TITLE header)"));
                }

                if (string.IsNullOrEmpty(song.Artist))
                {
                    errors.Add(new HeaderAnalyzeError("An artist is mandatory (#ARTIST header)"));
                }

                if (!song.Bpm.HasValue)
                {
                    errors.Add(new HeaderAnalyzeError("The song BPM is mandatory (#BPM header)"));
                }
                else if (song.Bpm.Value < 1)
                {
                    errors.Add(new HeaderAnalyzeError("The song BPM is less than 1"));
                }

                if (string.IsNullOrEmpty(song.Audio))
                {
                    errors.Add(new HeaderAnalyzeError("The song audio file is mandatory (#AUDIO header)"));
                }
                else if (Path.IsPathFullyQualified(song.Audio))
                {
                    errors.Add(new HeaderAnalyzeError("The song audio file must be a relative path"));
                }

                if (!string.IsNullOrEmpty(song.Video) && Path.IsPathFullyQualified(song.Video))
                {
                    errors.Add(new HeaderAnalyzeError("The song video file must be a relative path"));
                }

                if (!string.IsNullOrEmpty(song.Cover) && Path.IsPathFullyQualified(song.Cover))
                {
                    errors.Add(new HeaderAnalyzeError("The song cover file must be a relative path"));
                }

                if (!string.IsNullOrEmpty(song.Background) && Path.IsPathFullyQualified(song.Background))
                {
                    errors.Add(new HeaderAnalyzeError("The song background file must be a relative path"));
                }

                var songPlayers = song.GetPlayers();
                foreach (var songPlayer in songPlayers)
                {
                    if (string.IsNullOrEmpty(songPlayer.Value))
                    {
                        errors.Add(new HeaderAnalyzeError($"The player {songPlayer.Key} has no name defined (#P{songPlayer.Key} header)"));
                    }

                    if (songPlayer.Key > songPlayers.Count)
                    {
                        errors.Add(new HeaderAnalyzeError($"Prefer using player indexes by ascending order. #P{songPlayer.Key} could be replaced by a lower player number value", true));
                    }
                }

                if (!string.IsNullOrEmpty(song.AudioUrl) && !Uri.IsWellFormedUriString(song.AudioUrl, UriKind.Absolute))
                {
                    errors.Add(new HeaderAnalyzeError("The #AUDIOURL header should be a valid URL according to RFC 1738", true));
                }

                if (!string.IsNullOrEmpty(song.VideoUrl) && !Uri.IsWellFormedUriString(song.VideoUrl, UriKind.Absolute))
                {
                    errors.Add(new HeaderAnalyzeError("The #VIDEOURL header should be a valid URL according to RFC 1738", true));
                }

                if (!string.IsNullOrEmpty(song.CoverUrl) && !Uri.IsWellFormedUriString(song.CoverUrl, UriKind.Absolute))
                {
                    errors.Add(new HeaderAnalyzeError("The #COVERURL header should be a valid URL according to RFC 1738", true));
                }

                if (!string.IsNullOrEmpty(song.BackgroundUrl) && !Uri.IsWellFormedUriString(song.BackgroundUrl, UriKind.Absolute))
                {
                    errors.Add(new HeaderAnalyzeError("The #BACKGROUNDURL header should be a valid URL according to RFC 1738", true));
                }

                errors.AddRange(song.Languages.Where(l => !LanguagesHelper.IsValidLanguage(l)).Select(l =>
                    new HeaderAnalyzeError($"The language {l} is not an ISO 639.2 english name language", true)));

                errors.AddRange(song.Genres.Where(g => g.Any(c => !char.IsUpper(c))).Select(g =>
                    new HeaderAnalyzeError($"The genre {g} should be capitalized", true)));

                return errors;
            }, cancellationToken);
        }

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

                        if (analyzedNote.Duration < 1)
                        {
                            errors.Add(new NoteAnalyzeError("Duration is less than 1", analyzedNote));
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
                            continue;
                        }

                        if (!analyzedNote.Pitch.HasValue)
                        {
                            errors.Add(new NoteAnalyzeError($"The pitch is mandatory", analyzedNote));
                        }

                        if (string.IsNullOrEmpty(analyzedNote.Text))
                        {
                            errors.Add(new NoteAnalyzeError("A not empty text is mandatory", analyzedNote));
                        }

                        if (i == 0)
                        {
                            continue;
                        }

                        var previousNote = orderedPlayerNotes[i - 1];
                        var previousEndBeat = previousNote.StartBeat + previousNote.Duration - 1;
                        if (analyzedNote.StartBeat <= previousEndBeat)
                        {
                            errors.Add(new NoteAnalyzeError("There is an overlap with the previous note"));
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