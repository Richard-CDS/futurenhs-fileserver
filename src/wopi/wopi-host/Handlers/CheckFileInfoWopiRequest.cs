using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using System;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class CheckFileInfoWopiRequest
        : WopiRequest
    {
        private readonly string _fileId;
        private readonly Features _features;

        private CheckFileInfoWopiRequest(string fileId, string accessToken, Features features, CancellationToken cancellationToken) 
            : base(accessToken, isWriteAccessRequired: false, cancellationToken) 
        {
            if (string.IsNullOrWhiteSpace(fileId)) throw new ArgumentNullException(nameof(fileId));

            _fileId = fileId;
            _features = features;
        }

        internal static CheckFileInfoWopiRequest With(string fileId, string accessToken, Features features, CancellationToken cancellationToken) => new CheckFileInfoWopiRequest(fileId, accessToken, features, cancellationToken);

        protected override async Task HandleAsyncImpl(HttpContext context)
        {
            // GET /wopi/files/(file_id) 
            //
            // https://wopi.readthedocs.io/projects/wopirest/en/latest/files/CheckFileInfo.html#checkfileinfo
            // The CheckFileInfo operation is one of the most important WOPI operations. This operation must be implemented for all
            // WOPI actions. CheckFileInfo returns information about a file, a user’s permissions on that file, and general information
            // about the capabilities that the WOPI host has on the file. In addition, some CheckFileInfo properties can influence the 
            // appearance and behavior of WOPI clients.

            if (!(context.RequestServices.GetService(typeof(IWebHostEnvironment)) is IWebHostEnvironment hostingEnv)) return;

            if (hostingEnv is null) return;

            var filePath = Path.Combine(hostingEnv.ContentRootPath, "Files", _fileId);

            if (!File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath); 

            dynamic responseBody = new ExpandoObject();

            // Mandatory

            responseBody.BaseFileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
            responseBody.OwnerId = "richard.ashman@cds.co.uk";
            responseBody.Size = fileInfo.Length;
            responseBody.UserId = "richard@iprogrammer.co.uk";
            responseBody.Version = "1.0";

            // Host capabilities (defaults to false if excluded)

            var supportsUpdate = !(_features is null) && _features.AllowFileEdit;

            //responseBody.SupportedShareUrlType = new[] { "ReadOnly" }; // ReadOnly | ReadWrite
            //responseBody.SupportsCobalt = false;
            //responseBody.SupportsContainers = false;
            //responseBody.SupportsDeleteFile = false;
            //responseBody.SupportsEcosystem = false;
            //responseBody.SupportsExtendedLockLength = false;
            //responseBody.SupportsFolders = false;
            //responseBody.SupportsGetFileWopiSrc = false;
            //responseBody.SupportsGetLock = false;
            //responseBody.SupportsLocks = false;
            //responseBody.SupportsRename = false;
            responseBody.SupportsUpdate = supportsUpdate;
            //responseBody.SupportsUserInfo = false;

            // User metadata

            //responseBody.IsAnonymousUser = true;
            //responseBody.IsEduUser = false;
            //responseBody.LicenseCheckForEditIsEnabled = false;
            responseBody.UserFriendlyName = "Richard";

            // User permissions

            responseBody.ReadOnly = supportsUpdate;
            //responseBody.RestrictedWebViewOnly = false;
            //responseBody.UserCanAttend = false;
            //responseBody.UserCanNotWriteRelative = true;
            responseBody.UserCanPresent = true;
            //responseBody.UserCanRename = false;
            responseBody.UserCanWrite = supportsUpdate;

            // File URLs

            //responseBody.CloseUrl = string.Empty;
            //responseBody.DownloadUrl = string.Empty;
            //responseBody.FileEmbedCommandUrl = string.Empty;
            //responseBody.FileSharingUrl = string.Empty;
            //responseBody.FileUrl = string.Empty;
            //responseBody.FileVersionUrl = string.Empty;
            //responseBody.HostEditUrl = string.Empty;
            //responseBody.HostEmbeddedViewUrl = string.Empty;
            //responseBody.HostViewUrl = string.Empty;
            //responseBody.SignOutUrl = string.Empty;

            // PostMessage 

            responseBody.BreadcrumbBrandName = "CDS Ltd & FutureNHS";
            //responseBody.BreadcrumbBrandUrl = string.Empty;
            responseBody.BreadcrumbDocName = fileInfo.Name;
            responseBody.BreadcrumbFolderName = fileInfo.Directory.Name;
            //responseBody.BreadcrumbFolderUrl = string.Empty;

            // Miscellaneous

            responseBody.AllowAdditionalMicrosoftServices = true;
            responseBody.AllowErrorReportPrompt = false;
            responseBody.AllowExternalMarketplace = false;
            responseBody.ClientThrottlingProtection = "Normal"; // MostProtected | Protected | Normal | LessProtected | LeastProtected
            //responseBody.CloseButtonClosesWindow = false;
            //responseBody.CopyPasteRestrictions = "CurrentDocumentOnly"; // BlockAll | CurrentDocumentOnly
            responseBody.DisablePrint = false;
            responseBody.DisableTranslation = false;
            responseBody.FileExtension = fileInfo.Extension;
            responseBody.FileNameMaxLength = 250;
            responseBody.LastModifiedTime = fileInfo.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture); // "2021-04-19T13:00:00.0000000Z";
            responseBody.RequestedCallThrottling = "Normal"; // Normal | Minor | Medium | Major | Critical
                                                             //responseBody.SHA256 = 256 bit sha-2 encoded base 64 hash of the file contents
            responseBody.SharingStatus = "Private"; // Private | Shared
            //responseBody.UniqueContentId = "";

            // Collabora specific optional properties

            //responseBody.PostMessageOrigin = ;
            //responseBody.HidePrintOption = false;
            //responseBody.DisablePrint = false;
            //responseBody.HideSaveOption = false;
            //responseBody.HideExportOption = false;
            //responseBody.DisableExport = false;
            //responseBody.DisableCopy = false;
            //responseBody.EnableOwnerTermination = false;
            //responseBody.LastModifiedTime = "ISO8601";

            await context.Response.StartAsync();

            await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, responseBody);
        }
    }
}
