using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using KaraWeb.Core.Helpers;
using KaraWeb.Core.Persistence;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KaraWeb.Host
{
    internal sealed class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            var logger = ConfigureLog4NetAndGetLogger();
            if (!await EnsureDatabase(logger))
            {
                return;
            }

            var server = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .UseUrls(Constants.Uri)
                        .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
                });
            logger.Info($"Server is now started and listen at: {Constants.Uri}");
            await server.RunConsoleAsync();
        }

        private static ILog ConfigureLog4NetAndGetLogger()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(Constants.Log4NetConfigPath));
            return LogManager.GetLogger(Constants.ProjectName);
        }

        private static async Task<bool> EnsureDatabase(ILog logger)
        {
            try
            {
                if (!Directory.Exists(Constants.DataPath))
                {
                    Directory.CreateDirectory(Constants.DataPath);
                    logger.Info("Created data directory");
                }

                await using var context = new KaraWebDbContext();
                await context.Database.MigrateAsync();
                logger.Info("Database initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.Fatal($"Error when initializing the database: {ex}");
                return false;
            }
        }
    }
}