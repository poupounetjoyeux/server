using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace KaraWeb.Core.Models
{
    public sealed class Collection
    {
        [Key]
        [MaxLength(32)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Decription { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Path { get; set; }
    }
}
