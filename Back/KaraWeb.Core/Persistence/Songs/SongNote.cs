using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using KaraWeb.Shared.Models.Songs.Notes;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence.Songs
{
    [Table("SongNotes")]
    [Index(nameof(SongId))]
    public class SongNote : IAnalyzableSongNote
    {
        [Key]
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

        public List<string> Errors { get; set; } = new();

        [NotMapped]
        public bool HasError => Errors.Count > 0;

        public SongNoteDto ToDto()
        {
            return new SongNoteDto
            {
                Type = Type,
                PlayerNumber = PlayerNumber,
                StartBeat = StartBeat,
                Duration = Duration,
                Pitch = Pitch,
                Text = Text,
                Errors = Errors.ToList()
            };
        }
    }
}