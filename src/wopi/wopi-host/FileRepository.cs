using Azure.Identity;
using Azure.Storage.Blobs;
using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.Exceptions;
using FutureNHS.WOPIHost.PlatformHelpers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Net;
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
        /// <param name="streamToWriteTo">The stream to which the content of the file will be written in the success case/></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CopyToStreamAsync(string fileName, Stream streamToWriteTo, CancellationToken cancellationToken);
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

        async Task IFileRepository.CopyToStreamAsync(string fileName, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _azureBlobStoreClient.FetchBlobAndWriteToStream(_containerName, fileName, streamToWriteTo, cancellationToken);
        }


    }
}
