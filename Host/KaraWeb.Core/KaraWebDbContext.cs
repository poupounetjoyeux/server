using KaraWeb.Core.Helpers;
using KaraWeb.Core.Models.Collections;
using KaraWeb.Core.Models.Songs;
using KaraWeb.Core.Models.Songs.Notes;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core
{
    public sealed class KaraWebDbContext : DbContext
    {
        public DbSet<Collection> Collections { get; set; }

        public DbSet<Song> Songs { get; set; }

        public DbSet<SongNote> SongNotes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Constants.DbFilePath}");
        }
    }
}
