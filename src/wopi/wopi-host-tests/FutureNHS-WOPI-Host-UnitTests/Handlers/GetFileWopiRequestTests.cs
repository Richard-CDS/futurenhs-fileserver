using FutureNHS.WOPIHost;
using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests.Handlers
{
    [TestClass]
    public sealed class GetFileWopiRequestTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFilenameIsNull()
        {
            GetFileWopiRequest.With(fileName: default, fileVersion: "file-version", accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFilenameIsEmpty()
        {
            GetFileWopiRequest.With(fileName: string.Empty, fileVersion: "file-version", accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFilenameIsWhitespace()
        {
            GetFileWopiRequest.With(fileName: " ", fileVersion: "file-version", accessToken: "access-token");
        }

        [TestMethod]
        public void With_DoesNotThrowIfFilenameIsNotNullOrWhitespace()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsNull()
        {
            GetFileWopiRequest.With(fileName: "file-name", fileVersion: default, accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsEmpty()
        {
            GetFileWopiRequest.With(fileName: string.Empty, fileVersion: string.Empty, accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsWhitespace()
        {
            GetFileWopiRequest.With(fileName: " ", fileVersion: " ", accessToken: "access-token");
        }

        [TestMethod]
        public void With_DoesNotThrowIfFileVersionIsNotNullOrWhitespace()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: "access-token");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfAccessTokenIsNull()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: default);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfAccessTokenIsEmpty()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfAccessTokenIsWhitespace()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: " ");
        }

        [TestMethod]
        public void With_DoesNotThrowIfAccessTokenIsNotNullOrWhitespace()
        {
            GetFileWopiRequest.With("file-name", fileVersion: "file-version", accessToken: "access-token");
        }



        [TestMethod]
        [DataRow("Excel-Spreadsheet.xlsx")]
        [DataRow("Image-File.jpg")]
        [DataRow("OpenDocument-Text-File.odt")]
        [DataRow("Portable-Document-Format-File.pdf")]
        [DataRow("PowerPoint-Presentation.pptx")]
        [DataRow("Text-File.txt")]
        [DataRow("Word-Document.docx")]
        public async Task HandleAsync_ResolvesAndWritesFileCorrectlyToGivenStream(string fileName)
        {
            var cancellationToken = new CancellationToken();

            var httpContext = new DefaultHttpContext();

            var contentRootPath = Environment.CurrentDirectory;

            var filePath = Path.Combine(contentRootPath, "Files", fileName);

            Assert.IsTrue(File.Exists(filePath), $"Expected the {fileName} file to be accessible in the test environment");

            var fileBuffer = await File.ReadAllBytesAsync(filePath, cancellationToken);

            using var responseBodyStream = new MemoryStream(fileBuffer.Length);
            
            httpContext.Response.Body = responseBodyStream;

            var fileRepository = new Moq.Mock<IFileRepository>();

            var fileRepositoryInvoked = false;

            var services = new ServiceCollection();

            services.AddScoped(sp => fileRepository.Object);

            httpContext.RequestServices = services.BuildServiceProvider();

            var fileVersion = Guid.NewGuid().ToString();

            fileRepository.
                Setup(x => x.WriteToStreamAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<Stream>(), Moq.It.IsAny<CancellationToken>())).
                Callback(async (string givenFileName, string givenFileVersion, Stream givenStream, CancellationToken givenCancellationToken) => {

                    Assert.IsNotNull(givenFileName);
                    Assert.IsNotNull(givenFileVersion);
                    Assert.IsNotNull(givenStream);

                    Assert.IsFalse(givenCancellationToken.IsCancellationRequested, "Expected the cancellation token to not be cancelled");

                    Assert.AreSame(responseBodyStream, givenStream, "Expected the SUT to as the repository to write the file to the stream it was asked to");
                    Assert.AreSame(fileName, givenFileName, "Expected the SUT to request the file from the repository whose name it was provided with");
                    Assert.AreSame(fileVersion, givenFileVersion, "Expected the SUT to request the file version from the repository that it was provided with");
                    Assert.AreEqual(cancellationToken, givenCancellationToken, "Expected the same cancellation token to propagate between service interfaces");

                    await givenStream.WriteAsync(fileBuffer, cancellationToken);
                    await givenStream.FlushAsync(cancellationToken);

                    fileRepositoryInvoked = true;
                }).
                Returns(Task.CompletedTask);

            var accessToken = Guid.NewGuid().ToString();

            var getFileWopiRequest = GetFileWopiRequest.With(fileName, fileVersion, accessToken);

            await getFileWopiRequest.HandleAsync(httpContext, cancellationToken);

            Assert.IsTrue(fileRepositoryInvoked, "Expected the SUT to defer tp the file repository to load the file");

            Assert.AreEqual(fileBuffer.Length, responseBodyStream.Length, "All bytes in the file should be written to the target stream");

            Assert.IsTrue(httpContext.Response.Headers.ContainsKey("X-WOPI-ItemVersion"), "Expected the X-WOPI-ItemVersion header to have been written to the response");

            Assert.IsNotNull(httpContext.Response.Headers["X-WOPI-ItemVersion"], "Expected the X-WOPI-ItemVersion header in the response to not be null");
        }
    }
}
