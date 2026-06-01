using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Models.Songs
{
    /// <summary>
    /// A song's player defined by #P1 to #P9
    /// </summary>
    [Table("SongPlayers")]
    [PrimaryKey(nameof(SongId), nameof(Number))]
    public sealed class SongPlayer
    {
        [ForeignKey(nameof(Song))]
        [JsonIgnore]
        [Required]
        public Guid? SongId { get; set; }
        [JsonIgnore]
        public Song Song { get; set; }

        /// <summary>
        /// The player's number
        /// </summary>
        [Required]
        public int Number { get; set; }

        /// <summary>
        /// The player's name
        /// </summary>
        [MaxLength(200)]
        public string Name { get; set; }
    }
}
