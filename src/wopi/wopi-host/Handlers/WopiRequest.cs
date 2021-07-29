using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    public abstract class WopiRequest
    {
        protected WopiRequest() { }

        protected WopiRequest(
            string accessToken,
            bool isWriteAccessRequired
            )
        {
            _accessToken = accessToken ?? string.Empty;

            _isWriteAccessRequired = isWriteAccessRequired;
        }

        private readonly string _accessToken = string.Empty;
        private readonly bool _isWriteAccessRequired = false;

        internal bool IsEmpty => ReferenceEquals(this, WopiRequestFactory.EMPTY);

        internal Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
        {
            if (IsEmpty) throw new InvalidOperationException("An empty wopi request cannot handle an http context.  Check IsEmpty before invoking this method");

            cancellationToken.ThrowIfCancellationRequested();

            if (IsUnableToValidateAccessToken()) throw new ExpiredAccessTokenException("The access token has expired");

            return HandleAsyncImpl(context, cancellationToken);
        }

        protected abstract Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken);

        internal virtual bool IsUnableToValidateAccessToken() => IsEmpty;
    }
}
