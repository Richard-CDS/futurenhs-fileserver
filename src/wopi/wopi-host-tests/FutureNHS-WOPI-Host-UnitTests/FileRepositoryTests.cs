using FutureNHS.WOPIHost;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class FileRepositoryTests
    {
        // Given the dependency on Azure Storage, the system under test is difficult to cover in full without taking a dependency
        // on running emulators to serve/save files (which we cannot do in an Azure Pipeline).

        [TestMethod]
        public async Task GetAsync_()
        {
            //var cancellationToken = new CancellationToken();

            //var logger = new Moq.Mock<ILogger<FileRepository>>().Object;

            //IFileRepository fileRepository = new FileRepository(logger);

            //using var destinationStream = new MemoryStream();

            //try
            //{
            //    await fileRepository.CopyToAsync("4d6fa0f8-34a7-4f34-922f-8b06416097e1.pdf", destinationStream, cancellationToken);

            //    var fileBytes = destinationStream.ToArray();

            //    Assert.IsTrue(0 < fileBytes.Length);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }
    }
}
