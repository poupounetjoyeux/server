using System;

namespace KaraWeb.Core.Exceptions
{
    public class KaraWebException : Exception
    {
        public KaraWebException(string message) : base(message)
        {
        }
    }
}