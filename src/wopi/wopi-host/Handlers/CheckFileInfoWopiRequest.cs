using FutureNHS.WOPIHost.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.Handlers
{
    internal sealed class CheckFileInfoWopiRequest
        : WopiRequest
    {
        private const int FILENAME_MAXIMUM_LENGTH = 100;

        private readonly string _fileName;
        private readonly string? _fileVersion;
        private readonly Features? _features;

        private CheckFileInfoWopiRequest(string fileName, string? fileVersion, string accessToken, Features? features) 
            : base(accessToken, isWriteAccessRequired: false) 
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

            _fileName = fileName;
            _fileVersion = fileVersion;

            _features = features;
        }

        internal static CheckFileInfoWopiRequest With(string fileName, string? fileVersion, string accessToken, Features features) => new CheckFileInfoWopiRequest(fileName, fileVersion, accessToken, features);

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
            
            // TODO - How do we handle the null file version situation .. constructed file name that includes version or db lookup?

            //var fileMetadataRepository = context.RequestServices.GetRequiredService<IFileMetadataRepository>();


            //var fileMetadata = fileMetadataRepository.GetAsync(_fileName, _fileVersion, cancellationToken);


            dynamic responseBody = new ExpandoObject();

            // Mandatory

            responseBody.BaseFileName = "fileMetaData.Name"; // used as a display element in the UI
            responseBody.OwnerId = "richard.ashman@cds.co.uk";//fileMetaData.OwnerId  // uniquely identities the owner of the file - usage not yet clear but might be tied to collaborative editing feature
            responseBody.Size = 1; // fileMetaData.Size; // the size of the file in bytes
            responseBody.UserId = "richard@iprogrammer.co.uk"; //userMetadata.Id // uniquely identifies the user accessing the file - usage not yet clear but might be tied to collaborative editing feature

            // Where did this one come from - WOPI spec?
            responseBody.Version = "1.0"; //fileMetaData.VersionId;  

            // Host capabilities (defaults to false if excluded)

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

            responseBody.BreadcrumbBrandName = "FutureNHS";
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
 //           responseBody.FileExtension = fileInfo.Extension;
            responseBody.FileNameMaxLength = FILENAME_MAXIMUM_LENGTH;
 //           responseBody.LastModifiedTime = fileInfo.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture); // "2021-04-19T13:00:00.0000000Z";
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
            responseBody.LastModifiedTime = "ISO8601";

            await context.Response.StartAsync();

            await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, responseBody, cancellationToken: cancellationToken);
        }
    }
}
