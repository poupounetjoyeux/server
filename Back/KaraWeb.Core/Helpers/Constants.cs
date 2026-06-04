using System.IO;

namespace KaraWeb.Core.Helpers
{
    public static class Constants
    {
        public const string ProjectName = "KaraWeb";

        public const string ApiMainRoutePrefix = "api";
        public const int Port = 7373;

        public static readonly string DataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        public static readonly string ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "Config");
        public static readonly string Log4NetConfigPath = Path.Combine(ConfigPath, "config.log4net.xml");
        public static readonly string DbFilePath = Path.Combine(DataPath, "karaweb.db");
        public static readonly string Uri = "http://0.0.0.0:" + Port;
    }
}