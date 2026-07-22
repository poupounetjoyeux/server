using System;

namespace KaraW3B.Server.Songs.Models.Libraries
{
    /// <summary>
    ///     A songs library pointing to a folder containing songs
    /// </summary>
    public sealed class Library
    {
        /// <summary>
        ///     The library's ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        ///     The library's name
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        ///     The library's description
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        ///     The library's path to directory
        /// </summary>
        public string Path { get; init; }

        /// <summary>
        ///     The current status of the library analyze
        /// </summary>
        public LibraryAnalyzeStatus AnalyzeStatus { get; init; }

        /// <summary>
        ///     The last analyze message
        /// </summary>
        public string LastAnalyzeMessage { get; init; }
    }
}