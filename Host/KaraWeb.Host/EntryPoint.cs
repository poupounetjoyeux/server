using KaraWeb.Core;
using KaraWeb.Core.Helpers;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KaraWeb.Host
{
    public class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            var logger = ConfigureLog4NetAndGetLogger();
            if(!await EnsureDatabase(logger))
            {
                return;
            }

            var server = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(Constants.Uri);
            });
            await server.RunConsoleAsync();
        }

        private static ILog ConfigureLog4NetAndGetLogger()
        {
            return LogManager.GetLogger(Constants.ProjectName);
        }

        private static async Task<bool> EnsureDatabase(ILog logger)
        {
            try
            {
                if (!Directory.Exists(Constants.ConfigPath))
                {
                    Directory.CreateDirectory(Constants.ConfigPath);
                    logger.Info("Created config directory");
                }

                using (var context = new KaraWebDbContext())
                {
                    await context.Database.MigrateAsync();
                    logger.Info("Database initialized successfully");
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Fatal($"Error when intializing the database: {ex}");
                return false;
            }
        }
    }
}