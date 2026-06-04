using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence.Songs
{
    [Table("SongPlayers")]
    [PrimaryKey(nameof(SongId), nameof(Number))]
    public sealed class SongPlayer
    {
        [ForeignKey(nameof(Song))]
        public Guid SongId { get; set; }

        public int Number { get; set; }

        [MaxLength(200)]
        [Required]
        public string Name { get; set; }
    }
}