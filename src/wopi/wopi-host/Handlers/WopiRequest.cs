using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal abstract class WopiRequest
    {
        protected WopiRequest() { }
        protected WopiRequest(
            string accessToken,
            bool isWriteAccessRequired,
            CancellationToken cancellationToken
            )
        {
            _accessToken = accessToken ?? string.Empty;

            _isWriteAccessRequired = isWriteAccessRequired;

            _cancellationToken = cancellationToken;
        }

        private readonly string _accessToken = string.Empty;
        private readonly bool _isWriteAccessRequired = false;

        protected readonly CancellationToken _cancellationToken = CancellationToken.None;

        internal bool IsEmpty => ReferenceEquals(this, WopiRequestFactory.EMPTY);

        internal Task HandleAsync(HttpContext context)
        {
            if (IsEmpty) throw new InvalidOperationException("An empty wopi request cannot handle an http context.  Check IsEmpty before invoking this method");

            _cancellationToken.ThrowIfCancellationRequested();

            if (IsUnableToValidateAccessToken()) throw new ExpiredAccessTokenException("The access token has expired");

            return HandleAsyncImpl(context);
        }

        protected abstract Task HandleAsyncImpl(HttpContext context);

        internal virtual bool IsUnableToValidateAccessToken() => IsEmpty;
    }
}
