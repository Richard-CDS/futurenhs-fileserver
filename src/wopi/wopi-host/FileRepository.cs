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
        /// <param name="fileName">The name of the file as used in storage</param>
        /// <param name="fileVersion">The version of the file that the caller wishes to retrieve</param>
        /// <param name="streamToWriteTo">The stream to which the content of the file will be written in the success case/></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteToStreamAsync(string fileName, string fileVersion, Stream streamToWriteTo, CancellationToken cancellationToken);
    }

    public sealed class FileRepository : IFileRepository
    {
        private readonly IAzureBlobStoreClient _azureBlobStoreClient;
        private readonly IOptionsSnapshot<AzurePlatformConfiguration> _azurePlatformConfiguration;
        private readonly ILogger<FileRepository>? _logger;

        private readonly string _containerName;

        public FileRepository(IAzureBlobStoreClient azureBlobStoreClient, IOptionsSnapshot<AzurePlatformConfiguration> azurePlatformConfiguration, ILogger<FileRepository>? logger)
        {
            _logger = logger;

            _azureBlobStoreClient = azureBlobStoreClient             ?? throw new ArgumentNullException(nameof(azureBlobStoreClient));
            _azurePlatformConfiguration = azurePlatformConfiguration ?? throw new ArgumentNullException(nameof(azurePlatformConfiguration));

            var containerName = azurePlatformConfiguration.Value.AzureBlobStorage?.ContainerName;

            if (string.IsNullOrWhiteSpace(containerName)) throw new ApplicationException("The files container name is not set in the configuration");

            _containerName = containerName;
        }

        async Task IFileRepository.WriteToStreamAsync(string fileName, string fileVersion, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var downloadDetails = await _azureBlobStoreClient.FetchBlobAndWriteToStream(_containerName, fileName, fileVersion, streamToWriteTo, cancellationToken);

            // TODO - figure out useful information to return to the caller.  Might only be needed for file info requests but wondering 
            //        if we should store the hash when a doc is uploaded and then x-check it before allowing user to edit/download

            var contentType = downloadDetails.ContentType;

            var hash = downloadDetails.ContentHash;

            var etag = downloadDetails.ETag;

            var versionId = downloadDetails.VersionId;

        }


    }
}
