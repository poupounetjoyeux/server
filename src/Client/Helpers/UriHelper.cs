using System;

namespace KaraW3B.Client.Songs.Helpers
{
    public static class UriHelper
    {
        public static Uri AppendPath(this Uri baseUri, string path)
        {
            var baseUriStr = baseUri?.ToString();

            if (string.IsNullOrEmpty(baseUriStr))
            {
                return new Uri(path);
            }

            return string.IsNullOrEmpty(path)
                ? baseUri
                : new Uri($"{TrimSlashes(baseUriStr, false, true)}/{TrimSlashes(path, true, false)}", UriKind.Absolute);
        }

        public static string TrimSlashes(string uri, bool start, bool end)
        {
            return uri?.TrimEnd('/', '\\');
        }
    }
}