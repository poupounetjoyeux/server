using System.IO;

namespace KaraWeb.Host.Models
{
    /// <summary>
    /// The payload to create a collection
    /// </summary>
    public class CollectionPayload
    {
        /// <summary>
        /// The collection's name
        /// </summary>
        /// <remarks>Max length: 255</remarks>
        public string Name { get; set; }

        /// <summary>
        /// The collection's description
        /// </summary>
        /// <remarks>Max length: 2000</remarks>
        public string Description { get; set; }

        /// <summary>
        /// The collection's description
        /// </summary>
        /// <remarks>
        /// Max length: 2000
        /// Path must exist
        /// </remarks>
        public string Path { get; set; }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "A name is mandatory";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Path))
            {
                error = "A path is mandatory";
                return false;
            }
            else if (!Directory.Exists(Path))
            {
                error = $"The path '{Path}' doesn't exist";
                return false;
            }

            error = null;
            return true;
        }
    }
}
