using System;
using System.IO;
using KaraWeb.Shared;

namespace KaraWeb.SDK
{
    public sealed class LegacyFrameworkFileHelper : IFileHelper

    {
        /// <summary>
        ///     Beware that this method is not linux compatible
        /// </summary>
        public bool IsRelativePath(string path)
        {
            return !Path.IsPathRooted(path) ||
                   Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }
    }
}
