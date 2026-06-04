using KaraWeb.Core.Helpers;
using KaraWeb.Core.Persistence.Libraries;
using KaraWeb.Core.Persistence.Songs;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core.Persistence
{
    public sealed class KaraWebDbContext : DbContext
    {
        public DbSet<Library> Libraries { get; set; }

        public DbSet<Song> Songs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Constants.DbFilePath}");
        }
    }
}