using System.IO;

namespace KaraWeb.Core.Helpers
{
    public static class Constants
    {
        public const string ProjectName = "KaraWeb";

        public static readonly string ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config");
        public static readonly string DbFilePath = Path.Combine(ConfigPath, "karaweb.db");

        public const string ApiMainRoutePrefix = "api/";
        public const int Port = 7373;
        public static readonly string Uri = "http://0.0.0.0:" + Port;
    }
}