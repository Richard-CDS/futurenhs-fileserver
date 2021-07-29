using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    public interface IWopiRequestFactory
    {
        WopiRequest CreateRequest(HttpRequest request);
    }

    internal sealed class WopiRequestFactory
        : IWopiRequestFactory
    {
        internal static readonly WopiRequest EMPTY = new EmptyWopiRequest();

        WopiRequest IWopiRequestFactory.CreateRequest(HttpRequest request)
        {
            var path = request.Path;

            var features = request.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<Features>>();

            if (path.HasValue && path.StartsWithSegments("/wopi", StringComparison.OrdinalIgnoreCase))
            {
                var accessToken = request.Query["access_token"].FirstOrDefault();

                WopiRequest wopiRequest;

                if (path.StartsWithSegments("/wopi/files", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFileRequest(request.Method, path, accessToken, features?.Value);
                }
                else if (path.StartsWithSegments("/wopi/folders", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFolderRequest(request.Method, path, accessToken);
                }
                else return EMPTY;

                if (wopiRequest.IsUnableToValidateAccessToken()) return EMPTY;

                return wopiRequest;
            }

            return EMPTY;
        }

        private static WopiRequest IdentifyFileRequest(string method, PathString path, string accessToken, Features features)
        {
            var fileId = path.Value.Substring("/wopi/files/".Length)?.Trim();

            if (string.IsNullOrWhiteSpace(fileId)) return EMPTY;

            if (fileId.EndsWith("/contents"))
            {
                fileId = fileId.Substring(0, fileId.Length - "/contents".Length).Trim();

                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return GetFileWopiRequest.With(fileId, accessToken);
                }
                else if (0 == string.Compare("POST", method, StringComparison.OrdinalIgnoreCase))
                {
                    return PostFileWopiRequest.With(fileId, accessToken); ;
                }
            }
            else
            {
                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return CheckFileInfoWopiRequest.With(fileId, accessToken, features);
                }
            }

            return EMPTY;
        }

        private static WopiRequest IdentifyFolderRequest(string method, PathString path, string accessToken)
        {
            return EMPTY;
        }

        private sealed class EmptyWopiRequest
            : WopiRequest
        {
            internal EmptyWopiRequest() { }

            protected override Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}
