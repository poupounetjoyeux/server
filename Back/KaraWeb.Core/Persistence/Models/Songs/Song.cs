using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using KaraWeb.Core.Persistence.Models.Libraries;
using KaraWeb.Shared.Models.Songs;
using KaraWeb.Shared.Models.Songs.Files;
using KaraWeb.Shared.Models.Songs.Medleys;
using KaraWeb.Shared.Models.Songs.Messages;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence.Models.Songs
{
    [Table("Songs")]
    [PrimaryKey(nameof(Id))]
    public class Song : IAnalyzableSong
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [ForeignKey(nameof(Library))]
        public Guid LibraryId { get; set; }

        public virtual Library Library { get; set; }

        #region Core headers

        public Version Version { get; set; }

        public decimal Bpm { get; set; }

        [MaxLength(1000)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Artist { get; set; }

        public string Audio { get; set; }

        public TimeSpan? Gap { get; set; }

        public TimeSpan? Start { get; set; }

        public TimeSpan? End { get; set; }

        public virtual List<SongPlayer> Players { get; set; } = new();

        public Dictionary<int, string> GetPlayers()
        {
            return Players.ToDictionary(p => p.Number, p => p.Name);
        }

        #endregion

        #region Extra headers

        public string Cover { get; set; }

        public string Background { get; set; }

        public string Video { get; set; }

        public TimeSpan? VideoGap { get; set; }

        public string Vocals { get; set; }

        public string Instrumental { get; set; }

        public TimeSpan? PreviewStart { get; set; }

        public virtual SongMedley Medley { get; set; }

        public ISongMedley GetMedley()
        {
            return Medley;
        }

        public int? Year { get; set; }

        public List<string> Genres { get; set; } = new();

        public List<string> Languages { get; set; } = new();

        public List<string> Editions { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        public List<string> Creators { get; set; } = new();

        public string ProvidedBy { get; set; }

        public string Comment { get; set; }

        public string AudioUrl { get; set; }

        public string VideoUrl { get; set; }

        public string CoverUrl { get; set; }

        public string BackgroundUrl { get; set; }

        [MaxLength(300)]
        public string Rendition { get; set; }

        public List<string> NotManagedHeaders { get; set; } = new();

        #endregion

        #region Internal

        public virtual List<SongAlert> Alerts { get; set; } = new();

        public virtual List<SongNote> Notes { get; set; } = new();

        [Required]
        public string SongFilePath { get; set; }

        [NotMapped]
        public string SongDirectory => Path.GetDirectoryName(SongFilePath);

        [Required]
        public string AnalyzedFileHash { get; set; }

        [Required]
        public DateTime LastParseTime { get; set; }

        #endregion

        public string GetSongFilePath(FileType fileType)
        {
            var filePath = fileType switch
            {
                FileType.Audio => Audio,
                FileType.Cover => Cover,
                FileType.Background => Background,
                FileType.Video => Video,
                _ => null
            };

            return string.IsNullOrEmpty(filePath) ? null : Path.Combine(SongDirectory, filePath);
        }

        public bool SongFileExist(FileType fileType)
        {
            var songFilePath = GetSongFilePath(fileType);
            return !string.IsNullOrEmpty(songFilePath) && File.Exists(songFilePath);
        }

        private void FeedBaseSongDto(SongDtoBase songDto)
        {
            songDto.Id = Id;
            songDto.Version = Version;
            songDto.Bpm = Bpm;
            songDto.Title = Title;
            songDto.Artist = Artist;
            songDto.Gap = Gap;
            songDto.Start = Start;
            songDto.End = End;
            songDto.Players = Players.Select(p => p.ToDto()).ToList();
            songDto.VideoGap = VideoGap;
            songDto.PreviewStart = PreviewStart;
            songDto.Medley = Medley?.ToDto();
            songDto.Year = Year;
            songDto.Genres = Genres.ToList();
            songDto.Languages = Languages.ToList();
            songDto.Editions = Editions.ToList();
            songDto.Tags = Tags.ToList();
            songDto.Creators = Creators.ToList();
            songDto.ProvidedBy = ProvidedBy;
            songDto.Comment = Comment;
            songDto.Rendition = Rendition;
        }

        public SongDto ToDto()
        {
            var songDto = new SongDto
            {
                HasAudio = !string.IsNullOrEmpty(Audio),
                HasCover = !string.IsNullOrEmpty(Cover),
                HasBackground = !string.IsNullOrEmpty(Background),
                HasVideo = !string.IsNullOrEmpty(Video),
                HasVocals = !string.IsNullOrEmpty(Vocals),
                HasInstrumental = !string.IsNullOrEmpty(Instrumental),
                HasErrors = Alerts.Any(a => a.Level == AlertLevel.Error),
                HasWarnings = Alerts.Any(a => a.Level == AlertLevel.Warning)
            };
            FeedBaseSongDto(songDto);
            return songDto;
        }

        public DetailedSongDto ToDetailedDto()
        {
            var detailedSongDto = new DetailedSongDto
            {
                Audio = Audio,
                Cover = Cover,
                Background = Background,
                Video = Video,
                Vocals = Vocals,
                Instrumental = Instrumental,
                AudioUrl = AudioUrl,
                VideoUrl = VideoUrl,
                CoverUrl = CoverUrl,
                BackgroundUrl = BackgroundUrl,
                NotManagedHeaders = NotManagedHeaders.ToList(),
                Alerts = Alerts.Select(a => a.ToDto()).OrderBy(a => a.FileLine).ToList(),
                Notes = Notes.Select(n => n.ToDto()).OrderBy(n => n.PlayerNumber).ThenBy(n => n.StartBeat).ToList(),
                LastParsedTime = LastParseTime
            };
            FeedBaseSongDto(detailedSongDto);
            return detailedSongDto;
        }
    }
}