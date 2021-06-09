using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class PostFileWopiRequest
        : WopiRequest
    {
        private readonly string _fileId;

        private PostFileWopiRequest(string fileId, string accessToken, CancellationToken cancellationToken)
            : base(accessToken, isWriteAccessRequired: true, cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileId)) throw new ArgumentNullException(nameof(fileId));

            _fileId = fileId;
        }

        internal static PostFileWopiRequest With(string fileId, string accessToken, CancellationToken cancellationToken) => new PostFileWopiRequest(fileId, accessToken, cancellationToken);

        protected override async Task HandleAsyncImpl(HttpContext context)
        {
            // POST /wopi/files/(file_id)/content 

            // TODO - This is where we would update the file in our storage repository
            //        taking care of permissions, locking and versioning along the way 

            if (!(context.RequestServices.GetService(typeof(IWebHostEnvironment)) is IWebHostEnvironment hostingEnv)) return;

            if (hostingEnv is null) return;

            var filePath = Path.Combine(hostingEnv.ContentRootPath, "Files", _fileId);

            if (!File.Exists(filePath)) return;

            var pipeReader = context.Request.BodyReader;

            using var fileStrm = File.OpenWrite(filePath + "." + DateTime.UtcNow.ToFileTime());

            await context.Response.StartAsync(_cancellationToken);

            await pipeReader.CopyToAsync(fileStrm, _cancellationToken); // BUG: Isn't writing the whole file (why?)
        }
    }
}
