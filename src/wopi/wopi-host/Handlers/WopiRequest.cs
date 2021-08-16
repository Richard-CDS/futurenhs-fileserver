using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    public abstract class WopiRequest
    {
        internal static readonly WopiRequest EMPTY = new EmptyWopiRequest();

        private readonly bool _isWriteAccessRequired = false;

        protected WopiRequest() { }

        protected WopiRequest(string accessToken, bool isWriteAccessRequired)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            AccessToken = accessToken;

            _isWriteAccessRequired = isWriteAccessRequired;
        }

        public string AccessToken { get; }

        internal bool IsEmpty => ReferenceEquals(this, WopiRequest.EMPTY);

        internal Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
        {
            if (IsEmpty) throw new InvalidOperationException("An empty wopi request cannot handle an http context.  Check IsEmpty before invoking this method");

            cancellationToken.ThrowIfCancellationRequested();

            return HandleAsyncImpl(context, cancellationToken);
        }

        protected abstract Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken);

        /// <summary>
        /// TODO - This is where we need to implement our own token validation logic
        /// At the moment we have a fake guid being passed around but this need to be implemented properly for production
        /// </summary>
        /// <returns></returns>
        internal virtual bool IsUnableToValidateAccessToken() =>
            IsEmpty ||
            0 == string.Compare(AccessToken, "<invalid-access-token>", StringComparison.OrdinalIgnoreCase);  // TODO - Temporary to support unit tests
            
        private sealed class EmptyWopiRequest : WopiRequest
        {
            internal EmptyWopiRequest() { }

            protected override Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}
