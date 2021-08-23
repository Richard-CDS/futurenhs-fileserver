﻿using FutureNHS.WOPIHost;
using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.PlatformHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class FileRepositoryTests
    {
        // Given the dependency on Azure Storage, the system under test is difficult to cover in full without taking a dependency
        // on running emulators to serve/save files (which we cannot do in an Azure Pipeline) so the following tests are added to 
        // run in a debug (local) build

#if DEBUG
        [TestMethod]
        public async Task WriteToStreamAsync_SanityCheckForLocalDevOnly()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<FileRepository>>().Object;

            var azurePlatformConfiguration = new AzurePlatformConfiguration() { 
                AzureBlobStorage = new AzureBlobStorageConfiguration() { ContainerName = "files" }
            };

            var azurePlatformConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<AzurePlatformConfiguration>>();

            azurePlatformConfigurationOptionsSnapshot.Setup(x => x.Value).Returns(azurePlatformConfiguration);

            var primaryServiceUrl = new Uri("https://sacdsfnhsdevuksouthpub.blob.core.windows.net/", UriKind.Absolute);
            var geoRedundantServiceUrl = new Uri("https://sacdsfnhsdevuksouthpub-secondary.blob.core.windows.net/", UriKind.Absolute);

            var azureBlobStorageClient = new AzureBlobStoreClient(primaryServiceUrl, geoRedundantServiceUrl, default);

            IFileRepository fileRepository = new FileRepository(azureBlobStorageClient, azurePlatformConfigurationOptionsSnapshot.Object, logger);

            var file = File.With("4d6fa0f8-34a7-4f34-922f-8b06416097e1.pdf", "2021-08-09T18:15:02.4214747Z");

            using var destinationStream = new System.IO.MemoryStream();

            var fileWriteDetails = await fileRepository.WriteToStreamAsync(file, destinationStream, cancellationToken);

            Assert.IsNotNull(fileWriteDetails);

            var fileBytes = destinationStream.ToArray();

            Assert.IsTrue(396764 == fileBytes.Length);
            Assert.IsTrue(396764 == fileWriteDetails.ContentLength);

            Assert.AreEqual("8n45KHxmXabrze7rq/s9Ww==", fileWriteDetails.ContentHash);
        }
#endif
    }
}