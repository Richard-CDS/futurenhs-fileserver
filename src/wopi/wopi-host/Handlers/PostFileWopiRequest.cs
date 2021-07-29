using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

        private PostFileWopiRequest(string fileId, string accessToken)
            : base(accessToken, isWriteAccessRequired: true)
        {
            if (string.IsNullOrWhiteSpace(fileId)) throw new ArgumentNullException(nameof(fileId));

            _fileId = fileId;
        }

        internal static PostFileWopiRequest With(string fileId, string accessToken) => new PostFileWopiRequest(fileId, accessToken);

        protected override async Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken)
        {
            // POST /wopi/files/(file_id)/content 

            // TODO - This is where we would update the file in our storage repository
            //        taking care of permissions, locking and versioning along the way 

            var hostingEnv = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

            var filePath = Path.Combine(hostingEnv.ContentRootPath, "Files", _fileId);

            if (!File.Exists(filePath)) return;

            var pipeReader = context.Request.BodyReader;

            using var fileStrm = File.OpenWrite(filePath + "." + DateTime.UtcNow.ToFileTime());

            await context.Response.StartAsync(cancellationToken);

            await pipeReader.CopyToAsync(fileStrm, cancellationToken); // BUG: Isn't writing the whole file (why?)
        }
    }
}
