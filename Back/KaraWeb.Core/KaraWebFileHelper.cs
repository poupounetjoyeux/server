using System.IO;
using KaraWeb.Shared;

namespace KaraWeb.Core
{
    public sealed class KaraWebFileHelper : IFileHelper
    {
        public bool IsRelativePath(string path)
        {
            return !Path.IsPathFullyQualified(path);
        }
    }
}
