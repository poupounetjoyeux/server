using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraWeb.Shared.Models.Songs.Notes;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence.Songs
{
    [Table("SongNotes")]
    [PrimaryKey(nameof(SongId), nameof(Id))]
    public class SongNote : IAnalyzableSongNote
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public virtual Song Song { get; set; }

        [Required]
        public NoteType Type { get; set; }

        [Required]
        public int PlayerNumber { get; set; }

        [Required]
        public int StartBeat { get; set; }

        public int? Duration { get; set; }

        public int? Pitch { get; set; }

        public string Text { get; set; }

        public SongNoteDto ToDto()
        {
            return new SongNoteDto
            {
                Type = Type,
                PlayerNumber = PlayerNumber,
                StartBeat = StartBeat,
                Duration = Duration,
                Pitch = Pitch,
                Text = Text
            };
        }
    }
}