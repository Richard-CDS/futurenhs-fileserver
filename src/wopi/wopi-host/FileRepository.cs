using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.PlatformHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    public interface IFileRepository
    {
        /// <summary>
        /// Tasked with retrieving a file located in storage and writing it into <paramref name="streamToWriteTo"/>
        /// </summary>
        /// <param name="file">The name and version of the file to be used to locate the requested file in storage</param>
        /// <param name="streamToWriteTo">The stream to which the content of the file will be written in the success case/></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FileWriteDetails> WriteToStreamAsync(File file, Stream streamToWriteTo, CancellationToken cancellationToken);

        /// <summary>
        /// Tasked with retrieving the extended metadata for a specific file version
        /// </summary>
        /// <param name="file">The details of the file and version for which the extended metadata is being requested</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The requested metadata in the success case</returns>
        Task<FileMetadata> GetAsync(File file, CancellationToken cancellationToken);
    }

    public sealed class FileRepository : IFileRepository
    {
        private readonly IAzureBlobStoreClient _azureBlobStoreClient;
        private readonly IAzureSqlClient _azureSqlClient;
        private readonly ILogger<FileRepository>? _logger;

        private readonly string _blobContainerName;

        public FileRepository(IAzureBlobStoreClient azureBlobStoreClient, IAzureSqlClient azureSqlClient, IOptionsSnapshot<AzurePlatformConfiguration> azurePlatformConfiguration, ILogger<FileRepository>? logger)
        {
            _logger = logger;

            _azureBlobStoreClient = azureBlobStoreClient ?? throw new ArgumentNullException(nameof(azureBlobStoreClient));
            _azureSqlClient = azureSqlClient             ?? throw new ArgumentNullException(nameof(azureSqlClient));

            if (azurePlatformConfiguration?.Value is null) throw new ArgumentNullException(nameof(azurePlatformConfiguration));

            var blobContainerName = azurePlatformConfiguration.Value.AzureBlobStorage?.ContainerName;

            if (string.IsNullOrWhiteSpace(blobContainerName)) throw new ApplicationException("The files blob container name is not set in the configuration");

            _blobContainerName = blobContainerName;
        }

        async Task<FileMetadata> IFileRepository.GetAsync(File file, CancellationToken cancellationToken)
        {
            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            cancellationToken.ThrowIfCancellationRequested();

            // TODO - Implement properly and defer to a SQL client to do the heavy lifting

            var sb = new StringBuilder();

            sb.AppendLine($"SELECT   [Title]           = a.[Title]");
            sb.AppendLine($"       , [Description]     = a.[Description]");
            sb.AppendLine($"       , [Name]            = a.[FileName]");
            sb.AppendLine($"       , [Version]         = @FileVersion");            // TODO - Wire up when in database
            sb.AppendLine($"       , [SizeInBytes]     = a.[FileSize]");            // TODO - to be renamed in database
            sb.AppendLine($"       , [Extension]       = a.[FileExtension]");
            sb.AppendLine($"       , [BlobName]        = a.[FileUrl]");             // TODO - to be renamed in database
            sb.AppendLine($"       , [FileContentHash] = @FileContentHash");        // TODO - Wire up when in database
            sb.AppendLine($"       , [LastWriteTime]   = CONVERT(DATETIMEOFFSET, ISNULL(a.[ModifiedDate], a.[CreatedDate]))"); // TODO - DB data type needs changing to datetimeoffset, or datetime2 with renamed to suffix UTC so we know what it contains
            sb.AppendLine($"       , [FileStatus]      = a.[UploadStatus]");        // TODO - Earmarked to be renamed in DB to FileStatus
            sb.AppendLine($"       , [Owner]           = b.[UserName]");
            sb.AppendLine($"FROM   dbo.[File] a");
            sb.AppendLine($"JOIN   dbo.[MembershipUser] b ON b.[Id] = a.[CreatedBy]");
            sb.AppendLine($"WHERE  a.[Id] = @Id");

            var parameters = new { Id = file.Name, FileVersion = file.Version, FileContentHash = "replace-with-hash-code-soon" };

            var fileMetadata = await _azureSqlClient.GetRecord<FileMetadata>(sb.ToString(), parameters, cancellationToken);

            return fileMetadata;
        }

        async Task<FileWriteDetails> IFileRepository.WriteToStreamAsync(File file, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            var fileMetadata = await ((IFileRepository)this).GetAsync(file, cancellationToken);

            if (fileMetadata.IsEmpty) return FileWriteDetails.EMPTY;

            Debug.Assert(!string.IsNullOrWhiteSpace(fileMetadata.BlobName));
            Debug.Assert(!string.IsNullOrWhiteSpace(fileMetadata.Version));

            var downloadDetails = await _azureBlobStoreClient.FetchBlobAndWriteToStream(_blobContainerName, fileMetadata.BlobName, fileMetadata.Version, streamToWriteTo, cancellationToken);

            return new FileWriteDetails(
                version: downloadDetails.VersionId,
                contentHash: downloadDetails.ContentHash,               // https://blogs.msdn.microsoft.com/windowsazurestorage/2011/02/17/windows-azure-blob-md5-overview/
                contentEncoding: downloadDetails.ContentEncoding,
                contentLanguage: downloadDetails.ContentLanguage,
                contentType: downloadDetails.ContentType,
                contentLength: 0 > downloadDetails.ContentLength ? 0 : (ulong)downloadDetails.ContentLength,
                lastAccessed: DateTimeOffset.MinValue == downloadDetails.LastAccessed ? default : downloadDetails.LastAccessed,
                lastModified: downloadDetails.LastModified
                );
        }
    }
}
