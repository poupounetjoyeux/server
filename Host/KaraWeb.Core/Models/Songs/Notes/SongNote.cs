using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KaraWeb.Core.Models.Songs.Notes
{

    /// <summary>
    /// A song's note
    /// </summary>
    [Table("SongNotes")]
    public sealed class SongNote
    {
        /// <summary>
        /// The note's ID
        /// </summary>
        [Key]
        [JsonIgnore]
        public Guid NoteId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Song))]
        [JsonIgnore]
        [Required]
        public Guid? SongId { get; set; }
        [JsonIgnore]
        public Song Song { get; set; }

        /// <summary>
        /// The note's type
        /// </summary>
        [Required]
        public NoteType? Type { get; set; }

        /// <summary>
        /// The related player
        /// </summary>
        [Required]
        public int? PlayerNumber { get; set; }

        /// <summary>
        /// The note's start beat
        /// </summary>
        [Required]
        public int? StartBeat { get; set; }

        /// <summary>
        /// The note's duration
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// The note's pitch
        /// </summary>
        public int? Pitch { get; set; }

        /// <summary>
        /// The note's text
        /// </summary>
        public string Text { get; set; }
    }
}
