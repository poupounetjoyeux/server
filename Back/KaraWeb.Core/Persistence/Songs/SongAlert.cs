using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraWeb.Shared.Models.Songs.Messages;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence.Songs
{
    [Table("SongAlerts")]
    [Index(nameof(SongId))]
    public class SongAlert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public virtual Song Song { get; set; }

        [Required]
        public AlertType Type { get; set; }

        [Required]
        public string Message { get; set; }

        public int? NoteFileLine { get; set; }

        [NotMapped]
        public bool IsError => Type is AlertType.ParsingError or AlertType.HeaderError or AlertType.NoteError or AlertType.MissingFile;

        [NotMapped]
        public bool IsWarning => Type is AlertType.ParsingWarning or AlertType.HeaderWarning;

        public SongAlertDto ToDto()
        {
            return new SongAlertDto
            {
                Type = Type,
                Message = Message,
                NoteFileLine = NoteFileLine
            };
        }
    }
}