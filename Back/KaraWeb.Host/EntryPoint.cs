using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using KaraWeb.Core.Persistence;
using KaraWeb.Host.Helpers;
using KaraWeb.Shared.Helpers;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KaraWeb.Host
{
    internal sealed class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            var logger = ConfigureLog4NetAndGetLogger();
            if (!await KaraWebDbContext.EnsureDatabase(logger))
            {
                return;
            }

            var server = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .UseUrls(KaraWebApiConstants.Uri)
                        .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
                });
            logger.Info($"Server is now started and listen at: {KaraWebApiConstants.Uri}");
            await server.RunConsoleAsync();
        }

        private static ILog ConfigureLog4NetAndGetLogger()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(KaraWebApiConstants.Log4NetConfigPath));
            return LogManager.GetLogger(KaraWebConstants.Name);
        }
    }
}