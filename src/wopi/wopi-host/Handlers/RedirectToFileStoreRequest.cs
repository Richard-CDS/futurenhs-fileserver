using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class RedirectToFileStoreRequest : WopiRequest
    {
        private readonly File _file;

        private RedirectToFileStoreRequest(File file, string accessToken)
            : base(accessToken, isWriteAccessRequired: false, demandsProof: false)
        {
            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            _file = file;
        }

        internal static RedirectToFileStoreRequest With(File file, string accessToken) => new RedirectToFileStoreRequest(file, accessToken);

        protected override async Task HandleAsyncImpl(HttpContext httpContext, CancellationToken cancellationToken)
        {
            // This handler is tasked with generating an ephemeral link to our file storage (Azure blob store) location from 
            // where the target file can be directly downloaded (ie not having to be proxied through our servers) and then redirecting
            // the caller to it

            var fileRepository = httpContext.RequestServices.GetRequiredService<IFileRepository>();

            var uri = await fileRepository.GenerateEphemeralDownloadLink(_file, cancellationToken);

            if (uri is null) throw new ApplicationException($"Unable to generate an ephemeral download link for the file '{_file.Id}'");

            httpContext.Response.Redirect(uri.PathAndQuery, permanent: false);
        }
    }
}
