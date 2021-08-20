using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace FutureNHS.WOPIHost
{
    public interface IWopiRequestFactory
    {
        WopiRequest CreateRequest(HttpRequest request);
    }

    internal sealed class WopiRequestFactory
        : IWopiRequestFactory
    {
        private readonly IFileRepository _fileRepository;
        private readonly Features _features;
        private readonly ILogger<WopiRequestFactory>? _logger;

        public WopiRequestFactory(IFileRepository fileRepository, IOptionsSnapshot<Features> features, ILogger<WopiRequestFactory>? logger = default)
        {
            _fileRepository = fileRepository    ?? throw new ArgumentNullException(nameof(fileRepository));
            _features = features?.Value         ?? throw new ArgumentNullException(nameof(features.Value));

            _logger = logger;
        }

        WopiRequest IWopiRequestFactory.CreateRequest(HttpRequest httpRequest)
        {
            if (httpRequest is null) throw new ArgumentNullException(nameof(httpRequest));

            var path = httpRequest.Path;

            if (path.HasValue && path.StartsWithSegments("/wopi", StringComparison.OrdinalIgnoreCase))
            {
                var accessToken = httpRequest.Query["access_token"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(accessToken)) return WopiRequest.EMPTY; // TODO - Might be better to be more specific with a WopiRequest.MissingAccessToken response?

                WopiRequest wopiRequest;

                if (path.StartsWithSegments("/wopi/files", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFileRequest(httpRequest, path, accessToken, _features);
                }
                else if (path.StartsWithSegments("/wopi/folders", StringComparison.OrdinalIgnoreCase))
                {
                    wopiRequest = IdentifyFolderRequest();
                }
                else return WopiRequest.EMPTY;

                if (wopiRequest.IsUnableToValidateAccessToken()) return WopiRequest.EMPTY; // TODO - Might be better to be more specific with a WopiRequest.InvalidAccessToken response?

                return wopiRequest;
            }

            return WopiRequest.EMPTY;
        }

        private static WopiRequest IdentifyFileRequest(HttpRequest httpRequest, PathString path, string accessToken, Features features)
        {
            var fileName = path.Value.Substring("/wopi/files/".Length)?.Trim();

            if (string.IsNullOrWhiteSpace(fileName)) return WopiRequest.EMPTY;

            var method = httpRequest.Method;

            // NB - Collabora have not implemented support for the X-WOPI-ItemVersion header and so the Version field set in the 
            //      CheckFileInfo response does not flow through to those endpoints where it is optional - eg GetFile.
            //      This unfortunately means we have to do some crazy workaround using the fileId, and thus use that to derive 
            //      the relevant metadata needed for us to operate correctly.  Hopefully this will prove to be just a temporary
            //      workaround

            var version = "1.0";// - need to get this in the file name or use a db lookup;

            if (fileName.EndsWith("/contents"))
            {
                fileName = fileName.Substring(0, fileName.Length - "/contents".Length).Trim();

                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return GetFileWopiRequest.With(fileName, version, accessToken);
                }
                else if (0 == string.Compare("POST", method, StringComparison.OrdinalIgnoreCase))
                {
                    return PostFileWopiRequest.With(fileName, accessToken); ;
                }
            }
            else
            {
                if (0 == string.Compare("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return CheckFileInfoWopiRequest.With(fileName, version, accessToken, features);
                }
            }

            return WopiRequest.EMPTY;
        }

        private static WopiRequest IdentifyFolderRequest()
        {
            return WopiRequest.EMPTY;
        }
    }
}
