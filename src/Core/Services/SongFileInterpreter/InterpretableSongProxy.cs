using System;
using System.Collections.Generic;
using KaraW3B.Interpreters.Interfaces;
using KaraW3B.Server.Songs.Core.Persistence.Models.Songs;

namespace KaraW3B.Server.Songs.Core.Services.SongFileInterpreter
{
    internal sealed class InterpretableSongProxy : IInterpretableSong
    {
        private readonly DbSong _song;

        public InterpretableSongProxy(DbSong song)
        {
            _song = song;
        }

        public void AddPlayer(int playerNumber, string playerName)
        {
            _song.Players.Add(new DbSongPlayer { Number = playerNumber, Name = playerName });
        }

        public Dictionary<int, string> GetPlayers()
        {
            return _song.GetPlayers();
        }

        public void AddNote(ISongNote note)
        {
            _song.Notes.Add(new DbSongNote
            {
                NoteType = DbSongNote.ParseNoteType(note.Type),
                StartBeat = note.StartBeat,
                Duration = note.Duration,
                Pitch = note.Pitch,
                PlayerNumber = note.PlayerNumber,
                FileLine = note.FileLine,
                Text = note.Text
            });
        }

        public IReadOnlyCollection<ISongNote> GetNotes()
        {
            return _song.Notes;
        }

        public void SetMedley(ISongMedley medley)
        {
            _song.Medley = new DbSongMedley
            {
                MedleyEnd = medley.MedleyEnd,
                MedleyStart = medley.MedleyStart
            };
        }

        public ISongMedley GetMedley()
        {
            return _song.Medley;
        }

        public void Reset()
        {
            _song.Bpm = -1;
            _song.Title = null;
            _song.Artist = null;
            _song.Audio = null;
            _song.Gap = null;
            _song.Start = null;
            _song.End = null;
            _song.Players.Clear();

            _song.Cover = null;
            _song.Background = null;
            _song.Video = null;
            _song.VideoGap = null;
            _song.Vocals = null;
            _song.Instrumental = null;
            _song.PreviewStart = null;
            _song.Medley = null;
            _song.Year = null;

            _song.Genres.Clear();
            _song.Languages.Clear();
            _song.Editions.Clear();
            _song.Tags.Clear();
            _song.Creators.Clear();

            _song.ProvidedBy = null;
            _song.Comment = null;
            _song.AudioUrl = null;
            _song.VideoUrl = null;
            _song.CoverUrl = null;
            _song.BackgroundUrl = null;
            _song.Rendition = null;
            _song.NotManagedHeaders.Clear();

            _song.Alerts.Clear();
            _song.Notes.Clear();
        }

        public Version Version
        {
            get => _song.Version;
            set => _song.Version = value;
        }

        public string Title
        {
            get => _song.Title;
            set => _song.Title = value;
        }

        public string Artist
        {
            get => _song.Artist;
            set => _song.Artist = value;
        }

        public string Audio
        {
            get => _song.Audio;
            set => _song.Audio = value;
        }

        public decimal Bpm
        {
            get => _song.Bpm;
            set => _song.Bpm = value;
        }

        public TimeSpan? Gap
        {
            get => _song.Gap;
            set => _song.Gap = value;
        }

        public TimeSpan? Start
        {
            get => _song.Start;
            set => _song.Start = value;
        }

        public TimeSpan? End
        {
            get => _song.End;
            set => _song.End = value;
        }

        public string Video
        {
            get => _song.Video;
            set => _song.Video = value;
        }

        public TimeSpan? VideoGap
        {
            get => _song.VideoGap;
            set => _song.VideoGap = value;
        }

        public string Vocals
        {
            get => _song.Vocals;
            set => _song.Vocals = value;
        }

        public string Instrumental
        {
            get => _song.Instrumental;
            set => _song.Instrumental = value;
        }

        public TimeSpan? PreviewStart
        {
            get => _song.PreviewStart;
            set => _song.PreviewStart = value;
        }

        public string Cover
        {
            get => _song.Cover;
            set => _song.Cover = value;
        }

        public string Background
        {
            get => _song.Background;
            set => _song.Background = value;
        }

        public string AudioUrl
        {
            get => _song.AudioUrl;
            set => _song.AudioUrl = value;
        }

        public string VideoUrl
        {
            get => _song.VideoUrl;
            set => _song.VideoUrl = value;
        }

        public string CoverUrl
        {
            get => _song.CoverUrl;
            set => _song.CoverUrl = value;
        }

        public string BackgroundUrl
        {
            get => _song.BackgroundUrl;
            set => _song.BackgroundUrl = value;
        }

        public string Comment
        {
            get => _song.Comment;
            set => _song.Comment = value;
        }

        public string ProvidedBy
        {
            get => _song.ProvidedBy;
            set => _song.ProvidedBy = value;
        }

        public string Rendition
        {
            get => _song.Rendition;
            set => _song.Rendition = value;
        }

        public int? Year
        {
            get => _song.Year;
            set => _song.Year = value;
        }

        public List<string> Genres => _song.Genres;

        public List<string> Languages => _song.Languages;

        public List<string> Editions => _song.Editions;

        public List<string> Tags => _song.Tags;

        public List<string> Creators => _song.Creators;

        public List<string> NotManagedHeaders => _song.NotManagedHeaders;
    }
}
