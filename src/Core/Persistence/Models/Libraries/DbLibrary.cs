using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KaraW3B.Server.Songs.Models.Libraries;

namespace KaraW3B.Server.Songs.Core.Persistence.Models.Libraries
{
    [Table("Libraries")]
    public class DbLibrary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [Required]
        public string Path { get; set; }

        [Required]
        public LibraryAnalyzeStatus AnalyzeStatus { get; set; }

        public string LastAnalyzeMessage { get; set; }

        public Library ToLibrary()
        {
            return new Library
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Path = Path,
                AnalyzeStatus = AnalyzeStatus,
                LastAnalyzeMessage = LastAnalyzeMessage
            };
        }
    }
}