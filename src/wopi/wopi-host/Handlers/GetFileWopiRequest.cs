using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class GetFileWopiRequest
        : WopiRequest
    {
        private readonly string _fileId;

        private GetFileWopiRequest(string fileId, string accessToken, CancellationToken cancellationToken) 
            : base(accessToken, isWriteAccessRequired: false, cancellationToken) 
        {
            if (string.IsNullOrWhiteSpace(fileId)) throw new ArgumentNullException(nameof(fileId));
 
            _fileId = fileId;           
        }

        internal static GetFileWopiRequest With(string fileId, string accessToken, CancellationToken cancellationToken) => new GetFileWopiRequest(fileId, accessToken, cancellationToken);

        protected override async Task HandleAsyncImpl(HttpContext context)
        {
            // GET /wopi/files/(file_id)/content 

            // TODO - This is where we would go and get the file out of storage and write it to the response stream
            //        taking care of locking etc along the way if the user wants to edit it

            if (!(context.RequestServices.GetService(typeof(IWebHostEnvironment)) is IWebHostEnvironment hostingEnv)) return;

            if (hostingEnv is null) return;

            var filePath = Path.Combine(hostingEnv.ContentRootPath, "Files", _fileId);

            if (!File.Exists(filePath)) return;

            context.Response.Headers.Add("X-WOPI-ItemVersion", "1.0");

            await context.Response.StartAsync();

            await context.Response.SendFileAsync(filePath, _cancellationToken);
        }
    }
}
