using System;

namespace KaraWeb.Shared.Exceptions
{
    public class KaraWebException : Exception
    {
        public KaraWebException(string message) : base(message)
        {
        }
    }
}