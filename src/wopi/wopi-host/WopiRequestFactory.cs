using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    internal sealed class WopiRequestFactory
    {
        internal static readonly WopiRequest EMPTY = new EmptyWopiRequest();

        internal static WopiRequest CreateRequest(HttpRequest request, CancellationToken cancellationToken)
        {
            var path = request.Path;

            var features = request.HttpContext.RequestServices.GetService(typeof(IOptionsSnapshot<Features>)) as IOptionsSnapshot<Features>;

            if (path.HasValue && path.StartsWithSegments("/wopi", StringComparison.OrdinalIgnoreCase))
            {
                var accessToken = request.Query["access_token"].FirstOrDefault();

                WopiRequest wopiRequest;

                if (path.StartsWithSegments("/wopi/files", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFileRequest(request.Method, path, accessToken, features?.Value, cancellationToken);
                }
                else if (path.StartsWithSegments("/wopi/folders", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFolderRequest(request.Method, path, accessToken, cancellationToken);
                }
                else return EMPTY;

                if (wopiRequest.IsUnableToValidateAccessToken()) return EMPTY;

                return wopiRequest;
            }

            return EMPTY;
        }

        private static WopiRequest IdentifyFileRequest(string method, PathString path, string accessToken, Features features, CancellationToken cancellationToken)
        {
            var fileId = path.Value.Substring("/wopi/files/".Length)?.Trim();

            if (string.IsNullOrWhiteSpace(fileId)) return EMPTY;

            if (fileId.EndsWith("/contents"))
            {
                fileId = fileId.Substring(0, fileId.Length - "/contents".Length).Trim();

                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return GetFileWopiRequest.With(fileId, accessToken, cancellationToken);
                }
                else if (0 == string.Compare("POST", method, StringComparison.OrdinalIgnoreCase))
                {
                    return PostFileWopiRequest.With(fileId, accessToken, cancellationToken); ;
                }
            }
            else
            {
                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return CheckFileInfoWopiRequest.With(fileId, accessToken, features, cancellationToken);
                }
            }

            return EMPTY;
        }

        private static WopiRequest IdentifyFolderRequest(string method, PathString path, string accessToken, CancellationToken cancellationToken)
        {
            return EMPTY;
        }

        private sealed class EmptyWopiRequest
            : WopiRequest
        {
            internal EmptyWopiRequest() { }

            protected override Task HandleAsyncImpl(HttpContext context) => throw new NotImplementedException();
        }
    }
}
