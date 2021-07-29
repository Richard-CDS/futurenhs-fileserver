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
        private readonly string _fileId;

        private GetFileWopiRequest(string fileId, string accessToken) 
            : base(accessToken, isWriteAccessRequired: false) 
        {
            if (string.IsNullOrWhiteSpace(fileId)) throw new ArgumentNullException(nameof(fileId));
 
            _fileId = fileId;           
        }

        internal static GetFileWopiRequest With(string fileId, string accessToken) => new GetFileWopiRequest(fileId, accessToken);

        protected override async Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken)
        {
            // GET /wopi/files/(file_id)/content 

            // TODO - This is where we would go and get the file out of storage and write it to the response stream
            //        taking care of locking etc along the way if the user wants to edit it

            var hostingEnv = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

            var filePath = Path.Combine(hostingEnv.ContentRootPath, "Files", _fileId);

            if (!File.Exists(filePath)) return;

            context.Response.Headers.Add("X-WOPI-ItemVersion", "1.0");

            await context.Response.StartAsync();

            await context.Response.SendFileAsync(filePath, cancellationToken);
        }
    }
}
