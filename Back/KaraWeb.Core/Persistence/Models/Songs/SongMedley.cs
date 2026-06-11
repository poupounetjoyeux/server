using KaraWeb.Shared.Models.Songs.Medleys;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KaraWeb.Core.Persistence.Models.Songs
{
    [Table("SongMedleys")]
    public class SongMedley : ISongMedley
    {
        [Key]
        public Guid SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public virtual Song Song { get; set; }

        public TimeSpan MedleyStart { get; set; }

        public TimeSpan MedleyEnd { get; set; }

        public SongMedleyDto ToDto()
        {
            return new SongMedleyDto
            {
                MedleyStart = MedleyStart,
                MedleyEnd = MedleyEnd
            };
        }
    }
}
