using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests.Handlers
{
    [TestClass]
    public sealed class CheckFileInfoWopiRequestTests
    {
        [TestMethod]
        [DataRow("Excel-Spreadsheet.xlsx")]
        [DataRow("Image-File.jpg")]
        [DataRow("OpenDocument-Text-File.odt")]
        [DataRow("Portable-Document-Format-File.pdf")]
        [DataRow("PowerPoint-Presentation.pptx")]
        [DataRow("Text-File.txt")]
        [DataRow("Word-Document.docx")]
        public async Task a(string fileName)
        {
            var cancellationToken = new CancellationToken();

            var httpContext = new DefaultHttpContext();

            var features = new Features();

            var fileVersion = Guid.NewGuid().ToString();

            var accessToken = Guid.NewGuid().ToString();

            var checkFileInfoWopiRequest = CheckFileInfoWopiRequest.With(fileName, fileVersion, accessToken, features);

            await checkFileInfoWopiRequest.HandleAsync(httpContext, cancellationToken);
        }
    }
}
