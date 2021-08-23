using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.PlatformHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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
        private readonly ILogger<FileRepository>? _logger;

        private readonly string _containerName;

        public FileRepository(IAzureBlobStoreClient azureBlobStoreClient, IOptionsSnapshot<AzurePlatformConfiguration> azurePlatformConfiguration, ILogger<FileRepository>? logger)
        {
            _logger = logger;

            _azureBlobStoreClient = azureBlobStoreClient ?? throw new ArgumentNullException(nameof(azureBlobStoreClient));
            
            if (azurePlatformConfiguration?.Value is null) throw new ArgumentNullException(nameof(azurePlatformConfiguration));

            var containerName = azurePlatformConfiguration.Value.AzureBlobStorage?.ContainerName;

            if (string.IsNullOrWhiteSpace(containerName)) throw new ApplicationException("The files container name is not set in the configuration");

            _containerName = containerName;
        }

        async Task<FileMetadata> IFileRepository.GetAsync(File file, CancellationToken cancellationToken)
        {
            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            cancellationToken.ThrowIfCancellationRequested();

            // TODO - Implement properly

            return new FileMetadata("file-title", "file-description", file.Version, "file-owner", file.Name, "file-extension", 999, DateTimeOffset.UtcNow.AddDays(-1), "content-hash");
        }

        async Task<FileWriteDetails> IFileRepository.WriteToStreamAsync(File file, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.IsEmpty) throw new ArgumentNullException(nameof(file));

            var downloadDetails = await _azureBlobStoreClient.FetchBlobAndWriteToStream(_containerName, file.Name, file.Version, streamToWriteTo, cancellationToken);

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
