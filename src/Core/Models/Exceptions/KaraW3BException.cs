using System;

namespace KaraW3B.Server.Songs.Core.Models.Exceptions
{
    public class KaraW3BException : Exception
    {
        public KaraW3BException(string message) : base(message)
        {
        }
    }
}