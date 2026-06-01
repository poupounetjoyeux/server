using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KaraWeb.Core.Models.Collections
{
    /// <summary>
    /// A song collection folder
    /// </summary>
    [Table("Collections")]
    public sealed class Collection
    {
        /// <summary>
        /// The collection's ID
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The collection's name
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// The collection's description
        /// </summary>
        [MaxLength(2000)]
        public string Description { get; set; }

        /// <summary>
        /// The collection's path to directory
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Path { get; set; }
    }
}
