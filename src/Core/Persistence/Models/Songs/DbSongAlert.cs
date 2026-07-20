using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraW3B.Server.Songs.Models.Songs.Alerts;
using Microsoft.EntityFrameworkCore;

namespace KaraW3B.Server.Songs.Core.Persistence.Models.Songs
{
    [Table("SongAlerts")]
    [Index(nameof(SongId))]
    public class DbSongAlert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid SongId { get; set; }

        [ForeignKey(nameof(SongId))]
        public virtual DbSong Song { get; set; }

        [Required]
        public AlertType Type { get; set; }

        [Required]
        public AlertLevel Level { get; set; }

        [Required]
        public string Message { get; set; }

        public int? FileLine { get; set; }

        public SongAlert ToSongAlert()
        {
            return new SongAlert
            {
                Type = Type,
                Level = Level,
                Message = Message,
                FileLine = FileLine
            };
        }
    }
}