using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FutureNHS.WOPIHost.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost.PlatformHelpers
{
    public interface IAzureBlobStoreClient
    {
        Task<BlobDownloadDetails> FetchBlobAndWriteToStream(string containerName, string blobName, Stream streamToWriteTo, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Helper object that can be used to access Azure Blob Storage to perform common tasks
    /// </summary>
    /// <remarks>
    /// The identity being used to access Azure will need to have the appropriate
    /// permissions/role assigned to read content out of the target blob storage account/container combo.
    /// <b>Identity for authentication is discovered in the following order:</b>
    /// <list type="bullet">
    /// <item>Environment Vars</item>
    /// <item>Managed Identity if running in Azure</item>
    /// <item>Visual Studio (tools:options:azure service authentication:account selection)</item>
    /// <item>Azure CLI</item>
    /// <item>Azure Powershell</item>
    /// <item>Interactive (triggered with browser login)</item>
    /// </list>
    /// </remarks>
    public sealed class AzureBlobStoreClient : IAzureBlobStoreClient
    {
        private readonly ILogger<AzureBlobStoreClient>? _logger;

        private readonly Uri _primaryServiceUrl;
        private readonly Uri _geoRedundantServiceUrl;

        public AzureBlobStoreClient(Uri primaryServiceUrl, Uri geoRedundantServiceUrl, ILogger<AzureBlobStoreClient>? logger)
        {
            _primaryServiceUrl = primaryServiceUrl                   ?? throw new ArgumentNullException(nameof(primaryServiceUrl));
            _geoRedundantServiceUrl = geoRedundantServiceUrl         ?? throw new ArgumentNullException(nameof(geoRedundantServiceUrl));

            _logger = logger;
        }

        private static bool IsSuccessStatusCode(int statusCode) => statusCode >= 200 && statusCode <= 299;

        async Task<BlobDownloadDetails> IAzureBlobStoreClient.FetchBlobAndWriteToStream(string containerName, string blobName, Stream streamToWriteTo, CancellationToken cancellationToken)
        {
            // https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-msi
            // https://docs.microsoft.com/en-gb/dotnet/api/overview/azure/identity-readme

            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentNullException(nameof(blobName));

            if (streamToWriteTo is null) throw new ArgumentNullException(nameof(streamToWriteTo));

            cancellationToken.ThrowIfCancellationRequested();

            var managedIdentityCredential = new DefaultAzureCredential();

            var blobClientOptions = new BlobClientOptions { GeoRedundantSecondaryUri = _geoRedundantServiceUrl };

            // TODO - Set retry options in line with NFRs once they have been established with the client

            blobClientOptions.Retry.Delay = TimeSpan.FromMilliseconds(800);
            blobClientOptions.Retry.MaxDelay = TimeSpan.FromMinutes(1);
            blobClientOptions.Retry.MaxRetries = 5;
            blobClientOptions.Retry.Mode = Azure.Core.RetryMode.Exponential;
            blobClientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(100);

            blobClientOptions.Diagnostics.IsDistributedTracingEnabled = true;
            blobClientOptions.Diagnostics.IsLoggingContentEnabled = false;
            blobClientOptions.Diagnostics.IsLoggingEnabled = true;
            blobClientOptions.Diagnostics.IsTelemetryEnabled = true;

            var blobServiceClient = new BlobServiceClient(_primaryServiceUrl, managedIdentityCredential, blobClientOptions);

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var blobClient = containerClient.GetBlobClient(blobName);

            try
            {
                var result = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

                var response = result.GetRawResponse();

                if (!IsSuccessStatusCode(response.Status))
                {
                    _logger?.LogDebug($"Unable to download file from blob storage.  {response.ClientRequestId} - Reported '{ response.ReasonPhrase }' with status code: '{ response.Status } { Enum.Parse(typeof(HttpStatusCode), Convert.ToString(response.Status, CultureInfo.InvariantCulture)) }'");

                    throw new IrretrievableFileException($"{response.ClientRequestId}: Unable to download file from storage.  Please consult log files for more information");
                }

                await result.Value.Content.CopyToAsync(streamToWriteTo, cancellationToken);

                return result.Value.Details;
            }
            catch (AuthenticationFailedException ex)
            {
                _logger?.LogError(ex, "Unable to authenticate with the Azure Blob Storage service using the default credentials");

                throw;
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger?.LogError(ex, $"Unable to access the storage endpoint as the download request failed: '{ ex.Status } { Enum.Parse(typeof(HttpStatusCode), Convert.ToString(ex.Status, CultureInfo.InvariantCulture)) }'");

                throw;
            }
        }
    }
}
