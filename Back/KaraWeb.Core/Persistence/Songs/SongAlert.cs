using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraWeb.Shared.Models.Songs.Messages;

namespace KaraWeb.Core.Persistence.Songs
{
    [Table("SongAlerts")]
    public sealed class SongAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public AlertType Type { get; set; }

        [Required]
        public string Message { get; set; }

        [NotMapped]
        public bool IsError => Type is AlertType.ParsingError or AlertType.ValidationError or AlertType.MissingFile;

        [NotMapped]
        public bool IsWarning => Type is AlertType.ParsingWarning or AlertType.ValidationWarning;

        public SongAlertDto ToDto()
        {
            return new SongAlertDto
            {
                Type = Type,
                Message = Message
            };
        }
    }
}