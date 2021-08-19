using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class GetFileWopiRequest
        : WopiRequest
    {
        private readonly string _fileName;
        private readonly string _fileVersion;

        private GetFileWopiRequest(string fileName, string fileVersion, string accessToken) 
            : base(accessToken, isWriteAccessRequired: false) 
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileVersion)) throw new ArgumentNullException(nameof(fileVersion));

            _fileName = fileName;
            _fileVersion = fileVersion;
        }

        internal static GetFileWopiRequest With(string fileName, string fileVersion, string accessToken) => new GetFileWopiRequest(fileName, fileVersion, accessToken);

        protected override async Task HandleAsyncImpl(HttpContext httpContext, CancellationToken cancellationToken)
        {
            // GET /wopi/files/(file_id)/content 

            var fileRepository = httpContext.RequestServices.GetRequiredService<IFileRepository>();

            var httpResponse = httpContext.Response;

            // TODO - Get the item version from the metadata or use the blob version or etag to denote version number
            //        we'll use this when the WOPI client posts back updates to ensure someone else hasn't changed the file
            //        since it was opened for edit (assuming we don't implement a lock mechanism which we shouldn't as that 
            //        potentially makes collaborative editing more difficult - assuming we crack the server affinity problem)
            //
            //        Check whether the version presented to the wopi client from the check file info endpoint is actually 
            //        passed to this method and thus can be used by us to retrieve older versions

            httpResponse.Headers.Add("X-WOPI-ItemVersion", _fileVersion);

            var responseStream = httpResponse.Body; 

            // TODO - Clarify this is the right time to start sending the body given it also locks headers for modification etc.
            //        Unsure yet whether we need to include headers with information pulled from blob storage or the file metadata 
            //        held in the database.  Going to start it before we pull from blob storage on the assumption (to be tested) 
            //        that this means for larger files we won't have to reserve enough memory/disk space to hold the complete file
            //        on this server

            await httpResponse.StartAsync(cancellationToken);

            await fileRepository.WriteToStreamAsync(_fileName, _fileVersion, responseStream, cancellationToken);

            await responseStream.FlushAsync(cancellationToken);
        }
    }
}
