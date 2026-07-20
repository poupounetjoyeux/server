using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraW3B.Server.Songs.Models.Songs;
using Microsoft.EntityFrameworkCore;

namespace KaraW3B.Server.Songs.Core.Persistence.Models.Songs
{
    [Table("SongPlayers")]
    [PrimaryKey(nameof(SongId), nameof(Number))]
    public class DbSongPlayer
    {
        public Guid SongId { get; set; }

        [ForeignKey(nameof(SongId))]
        public virtual DbSong Song { get; set; }

        [Required]
        public int Number { get; set; }

        [MaxLength(200)]
        [Required]
        public string Name { get; set; }

        public SongPlayer ToSongPlayer()
        {
            return new SongPlayer
            {
                Name = Name,
                PlayerNumber = Number
            };
        }
    }
}