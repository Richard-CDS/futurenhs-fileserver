using System;

namespace FutureNHS.WOPIHost
{
    internal sealed class ExpiredAccessTokenException
        : ApplicationException
    {
        public ExpiredAccessTokenException(string message) : base(message) { }
    }
}
