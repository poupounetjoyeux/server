using KaraWeb.Core.Helpers;
using KaraWeb.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KaraWeb.Core
{
    public sealed class KaraWebDbContext : DbContext
    {
        public DbSet<Collection> Collections { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Constants.DbFilePath}");
        }
    }
}
