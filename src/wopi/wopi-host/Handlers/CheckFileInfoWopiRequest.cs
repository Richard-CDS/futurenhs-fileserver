using FutureNHS.WOPIHost.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class CheckFileInfoWopiRequest
        : WopiRequest
    {
        private readonly File _file;
        private readonly Features? _features;

        private CheckFileInfoWopiRequest(File file, string accessToken, Features? features) 
            : base(accessToken, isWriteAccessRequired: false) 
        {
            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            _file = file;

            _features = features;
        }

        internal static CheckFileInfoWopiRequest With(File file, string accessToken, Features features) => new CheckFileInfoWopiRequest(file, accessToken, features);

        protected override async Task HandleAsyncImpl(HttpContext context, CancellationToken cancellationToken)
        {
            // GET /wopi/files/(file_id) 
            //
            // https://wopi.readthedocs.io/projects/wopirest/en/latest/files/CheckFileInfo.html#checkfileinfo
            // The CheckFileInfo operation is one of the most important WOPI operations. This operation must be implemented for all
            // WOPI actions. CheckFileInfo returns information about a file, a user’s permissions on that file, and general information
            // about the capabilities that the WOPI host has on the file. In addition, some CheckFileInfo properties can influence the 
            // appearance and behavior of WOPI clients.
            //
            // NB - Collabora does not implement the full WOPI specification and thus only meets the bare minimum specification.
            //      You can find more information in their 'Integration of Collabora Online with WOPI' document
            
            var fileRepository = context.RequestServices.GetRequiredService<IFileRepository>();

            var fileMetadata = await fileRepository.GetAsync(_file, cancellationToken);

            if (fileMetadata.IsEmpty) throw new ApplicationException("The file metadata could not be found.  Please ensure the file is known to the application, or wait a few minutes for any database synchronisation activities to complete.  Alternatively report the issue to our support team so we can investigate if data has been lost as a result of a recent database restore operation.");

            if (FileStatus.Verified != fileMetadata.FileStatus) throw new ApplicationException($"The status of the file '{fileMetadata.FileStatus}' does not indicate it is yet safe to be shared with users.");

            // TODO - Get user context from the authenticated user


            dynamic responseBody = new ExpandoObject();

            // Mandatory

            responseBody.BaseFileName = fileMetadata.Title; // used as a display element in the UI
            responseBody.OwnerId = fileMetadata.Owner;  // uniquely identities the owner of the file - usage not yet clear but might be tied to collaborative editing feature
            responseBody.Size = fileMetadata.SizeInBytes; // the size of the file in bytes
            responseBody.UserId = "richard@iprogrammer.co.uk"; //userMetadata.Id // uniquely identifies the user accessing the file - usage not yet clear but might be tied to collaborative editing feature

            // Where did this one come from - WOPI spec?
            responseBody.Version = fileMetadata.Version;  

            // Host capabilities (defaults to false if excluded)
            // TODO - Pull from feature configuration and also user context

            var supportsUpdate = !(_features is null) && _features.AllowFileEdit;

            responseBody.SupportedShareUrlType = new[] { "ReadOnly" }; // ReadOnly | ReadWrite
            responseBody.SupportsCobalt = false;
            responseBody.SupportsContainers = false;
            responseBody.SupportsDeleteFile = false;
            responseBody.SupportsEcosystem = false;
            responseBody.SupportsExtendedLockLength = false;
            responseBody.SupportsFolders = false;
            responseBody.SupportsGetFileWopiSrc = false;
            responseBody.SupportsGetLock = false;
            responseBody.SupportsLocks = false;
            responseBody.SupportsRename = false;
            responseBody.SupportsUpdate = supportsUpdate;
            responseBody.SupportsUserInfo = false;

            // User metadata

            responseBody.IsAnonymousUser = false;
            responseBody.IsEduUser = false;
            responseBody.LicenseCheckForEditIsEnabled = false;
            responseBody.UserFriendlyName = "Richard";

            // User permissions

            responseBody.ReadOnly = !supportsUpdate;
            responseBody.RestrictedWebViewOnly = false;
            responseBody.UserCanAttend = false;
            responseBody.UserCanNotWriteRelative = true;    // stops use of SaveAs feature for creating a new file on our server, which isn't something we yet support
            responseBody.UserCanPresent = true;
            responseBody.UserCanRename = false;
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

            responseBody.BreadcrumbBrandName = "FutureNHS Open"; // TODO - pull from config file
            responseBody.BreadcrumbBrandUrl = string.Empty;
 //           responseBody.BreadcrumbDocName = fileInfo.Name;
 //           responseBody.BreadcrumbFolderName = fileInfo.Directory.Name;
            responseBody.BreadcrumbFolderUrl = string.Empty;

            // Miscellaneous

            responseBody.AllowAdditionalMicrosoftServices = false;
            responseBody.AllowErrorReportPrompt = false;
            responseBody.AllowExternalMarketplace = false;
            responseBody.ClientThrottlingProtection = "Normal"; // MostProtected | Protected | Normal | LessProtected | LeastProtected
            responseBody.CloseButtonClosesWindow = false;
            responseBody.CopyPasteRestrictions = "CurrentDocumentOnly"; // BlockAll | CurrentDocumentOnly
            responseBody.DisablePrint = false;
            responseBody.DisableTranslation = false;
            responseBody.FileExtension = fileMetadata.Extension;
            responseBody.FileNameMaxLength = File.FILENAME_MAXIMUM_LENGTH;
            responseBody.LastModifiedTime = fileMetadata.LastWriteTimeIso8601; // "2021-04-19T13:00:00.0000000Z";
            responseBody.RequestedCallThrottling = "Normal"; // Normal | Minor | Medium | Major | Critical
            //responseBody.SHA256 = 256 bit sha-2 encoded base 64 hash of the file contents
            responseBody.SharingStatus = "Private"; // Private | Shared
            //responseBody.UniqueContentId = "";

            // Collabora specific optional properties - do we need to make this configurable and thus use feature flags for them?

            //responseBody.PostMessageOrigin = ;
            responseBody.HidePrintOption = false;
            responseBody.DisablePrint = false;
            responseBody.HideSaveOption = false;
            responseBody.HideExportOption = false;
            responseBody.DisableExport = false;
            responseBody.DisableCopy = false;
            responseBody.EnableOwnerTermination = false;

            context.Response.ContentType = "application/json";

            await context.Response.StartAsync();

            await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, responseBody, cancellationToken: cancellationToken);
        }
    }
}
