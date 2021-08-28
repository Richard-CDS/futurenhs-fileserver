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
        private readonly File _file;

        private GetFileWopiRequest(File file, string accessToken) 
            : base(accessToken, isWriteAccessRequired: false) 
        {
            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            _file = file;
        }

        internal static GetFileWopiRequest With(File file, string accessToken) => new GetFileWopiRequest(file, accessToken);

        protected override async Task HandleAsyncImpl(HttpContext httpContext, CancellationToken cancellationToken)
        {
            // GET /wopi/files/(file_id)/content 

            var fileRepository = httpContext.RequestServices.GetRequiredService<IFileRepository>();

            var httpResponse = httpContext.Response;

            // NB - Collabora does not currently support the ItemVersion header but we'll add it here for completeness and hopefully
            //      come back and use it once Collabora echoes it back to our CheckFileInfo endpoint (etc)

            httpResponse.Headers.Add("X-WOPI-ItemVersion", _file.Version);

            var responseStream = httpResponse.Body; 

            // TODO - Clarify this is the right time to start sending the body given it also locks headers for modification etc.
            //        Unsure yet whether we need to include headers with information pulled from blob storage or the file metadata 
            //        held in the database.  Going to start it before we pull from blob storage on the assumption (to be tested) 
            //        that this means for larger files we won't have to reserve enough memory/disk space to hold the complete file
            //        on this server

            await httpResponse.StartAsync(cancellationToken);

            var fileWriteDetails = await fileRepository.WriteToStreamAsync(_file, responseStream, cancellationToken);

            if (fileWriteDetails.IsEmpty) throw new ApplicationException("Unable to load metadata for the requested file and version");

            // TODO - Given we are writing direct to the response stream, is there a possibility that the wrong blob is sent to the client if the hash or version information 
            //        of the blob used do not match with that of the blob we requested and thus expected?  Will throwing an exception after the event, result in some 
            //        of the file being streamed (for larger files) before the response completes?  Need to test this thoroughly otherwise we may be at risk of sharing 
            //        a file that the authenticated user is neither interested in, nor perhaps has the rights to access, or even worse, has been tampered with!

            if (!string.Equals(fileWriteDetails.Version, _file.Version, StringComparison.OrdinalIgnoreCase)) throw new ApplicationException("The blob store client returned a version of the blob that does not match the version requested");

            // Verify the hash stored in the database when the version was created is the same as the one of the file we just downloaded

            var fileMetadata = fileWriteDetails.FileMetadata;

            if (fileMetadata.IsEmpty) throw new ApplicationException("The file metadata could not be found for a file that has been located in storage.  Please ensure the file is known to the application, or wait a few minutes for any database synchronisation activities to complete.  Alternatively report the issue to our support team so we can investigate if data has been lost as a result of a recent database restore operation.");

            if (FileStatus.Verified != fileMetadata.FileStatus) throw new ApplicationException($"The status of the file '{fileMetadata.FileStatus}' does not indicate it is safe to be shared with users.");

            if (fileMetadata.ContentHash != fileWriteDetails.ContentHash) throw new ApplicationException("The hash of the stored file does not match the has of the version downloaded from storage.  The file may have been tampered with and will not be returned to the requestor");

            // Done reading, so make sure we are done writing too

            await responseStream.FlushAsync(cancellationToken);
        }
    }
}
