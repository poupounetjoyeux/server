using System.IO;

namespace KaraW3B.Server.Songs.Models.Libraries
{
    /// <summary>
    ///     The payload to create a library
    /// </summary>
    public class LibraryCreationPayload
    {
        /// <summary>
        ///     The library's name
        /// </summary>
        /// <remarks>Max length: 255</remarks>
        public string Name { get; set; }

        /// <summary>
        ///     The library's description
        /// </summary>
        /// <remarks>Max length: 2000</remarks>
        public string Description { get; set; }

        /// <summary>
        ///     The library's description
        /// </summary>
        /// <remarks>
        ///     Max length: 2000
        ///     Path must exist
        /// </remarks>
        public string Path { get; set; }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrEmpty(Name))
            {
                error = "A name is mandatory";
                return false;
            }

            if (string.IsNullOrEmpty(Path))
            {
                error = "A path is mandatory";
                return false;
            }

            if (!Directory.Exists(Path))
            {
                error = $"The path '{Path}' doesn't exist";
                return false;
            }

            error = null;
            return true;
        }
    }
}