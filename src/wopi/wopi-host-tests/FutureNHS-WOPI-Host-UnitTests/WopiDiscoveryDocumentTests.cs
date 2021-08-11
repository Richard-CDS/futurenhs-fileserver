using FutureNHS.WOPIHost;
using FutureNHS_WOPI_Host_UnitTests.Stubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class WopiDiscoveryDocumentTests
    {
        const string WOPI_ROOT = "https://futurenhs.cds.co.uk/gateway/wopi/";
        const string WOPI_SRC = WOPI_ROOT + "host/files/documentnamegoeshere.docx";
        const string WOPI_DISCOVERY_DOCUMENT_XML = 
            "<wopi-discovery>" + 
              "<net-zone name=\"external-http\">" + 
                "<app name=\"writer\">" + 
                  "<action default=\"true\" ext=\"docx\" name=\"edit\" urlsrc=\"" + WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?\"></action>" + 
                "</app>" + 
              "</net-zone>" +
              "<proof-key exponent=\"AQAB\" " +
                         "modulus=\"0TAzPoRjdY14NelqBGwJTnrFHuAuowoxUJvIayDZhi6DhyPUPYTT/zM9o5MJC4PVl7EduQSqTRunjZobFuDp7zMX9HmzkAwTZn07tpxEL7jYIy8a2+Rkl7KqEE0LQVCWQxS41vXOtxCgY81BJalnHEKXMQpO1Emjc2EBoOTJBPjf4iUILqMKE+RhKqs/ifXD/4n9IZzErAiIrQK93EsOhHqIabY/LDPpI2B4rNbnlC8Gf+HvFWkul/UsLdw6RcSjP3vGrHfYORmIafzWooAtVcTx2HAeydLgGlQq9DvahvoVB0rz3sYOmPW+L45a12t57ig1V/T+fuukYqBg2XTbxQ==\" " +
                         "oldexponent=\"AQAB\" oldmodulus=\"0TAzPoRjdY14NelqBGwJTnrFHuAuowoxUJvIayDZhi6DhyPUPYTT/zM9o5MJC4PVl7EduQSqTRunjZobFuDp7zMX9HmzkAwTZn07tpxEL7jYIy8a2+Rkl7KqEE0LQVCWQxS41vXOtxCgY81BJalnHEKXMQpO1Emjc2EBoOTJBPjf4iUILqMKE+RhKqs/ifXD/4n9IZzErAiIrQK93EsOhHqIabY/LDPpI2B4rNbnlC8Gf+HvFWkul/UsLdw6RcSjP3vGrHfYORmIafzWooAtVcTx2HAeydLgGlQq9DvahvoVB0rz3sYOmPW+L45a12t57ig1V/T+fuukYqBg2XTbxQ==\" " +
                         "oldvalue=\"BgIAAACkAABSU0ExAAgAAAEAAQDF23TZYKBipOt+/vRXNSjueWvXWo4vvvWYDsbe80oHFfqG2jv0KlQa4NLJHnDY8cRVLYCi1vxpiBk52Hesxns/o8RFOtwtLPWXLmkV7+F/Bi+U59aseGAj6TMsP7ZpiHqEDkvcvQKtiAisxJwh/Yn/w/WJP6sqYeQTCqMuCCXi3/gEyeSgAWFzo0nUTgoxl0IcZ6klQc1joBC3zvXWuBRDllBBC00QqrKXZOTbGi8j2LgvRJy2O31mEwyQs3n0FzPv6eAWG5qNpxtNqgS5HbGX1YMLCZOjPTP/04Q91COHgy6G2SBryJtQMQqjLuAexXpOCWwEauk1eI11Y4Q+MzDR\" " +
                         "value=\"BgIAAACkAABSU0ExAAgAAAEAAQDF23TZYKBipOt+/vRXNSjueWvXWo4vvvWYDsbe80oHFfqG2jv0KlQa4NLJHnDY8cRVLYCi1vxpiBk52Hesxns/o8RFOtwtLPWXLmkV7+F/Bi+U59aseGAj6TMsP7ZpiHqEDkvcvQKtiAisxJwh/Yn/w/WJP6sqYeQTCqMuCCXi3/gEyeSgAWFzo0nUTgoxl0IcZ6klQc1joBC3zvXWuBRDllBBC00QqrKXZOTbGi8j2LgvRJy2O31mEwyQs3n0FzPv6eAWG5qNpxtNqgS5HbGX1YMLCZOjPTP/04Q91COHgy6G2SBryJtQMQqjLuAexXpOCWwEauk1eI11Y4Q+MzDR\"" +
              "/>" +
            "</wopi-discovery>";

#if DEBUG

        [TestMethod]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<UNKNOWN_PLACEHOLDER=PLACEHOLDER_VALUE>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<UNKNOWN_PLACEHOLDER=PLACEHOLDER_VALUE[&]><UNKNOWN_PLACEHOLDER=PLACEHOLDER_VALUE>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<WOPI_SRC=PLACEHOLDER_VALUE>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?WOPI_SRC=" + WOPI_ROOT + "host/files/filenamegoeshere.docx")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<WOPI_SRC=PLACEHOLDER_VALUE[&]>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?WOPI_SRC=" + WOPI_ROOT + "host/files/filenamegoeshere.docx")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<WOPI_SRC=PLACEHOLDER_VALUE[&]><WOPI_SRC=PLACEHOLDER_VALUE>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?WOPI_SRC=" + WOPI_ROOT + "host/files/filenamegoeshere.docx&WOPI_SRC=" + WOPI_ROOT + "host/files/filenamegoeshere.docx")]
        [DataRow(WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?<WOPI_SRC=PLACEHOLDER_VALUE[&]><UNKNOWN_PLACEHOLDER=PLACEHOLDER_VALUE>", WOPI_ROOT + "host/files/filenamegoeshere.docx", WOPI_ROOT + "client/loleaflet/4aa2794/loleaflet.html?WOPI_SRC=" + WOPI_ROOT + "host/files/filenamegoeshere.docx")]
        public void TransformActionUrlSrcAttribute_CorrectlyReplacesAndRemovesPlaceholders(
            string urlSrc,
            string wopiSrc,
            string expectedUrlSrc
            )
        {
            var transformedUrlSrc = WopiDiscoveryDocument.TransformActionUrlSrcAttribute(urlSrc, new Uri(wopiSrc, UriKind.Absolute));

            Assert.AreEqual(expectedUrlSrc, transformedUrlSrc);
        }

#endif

        [TestMethod]
        public async Task GetAsync_ReturnsEmptyDocumentWhenAbsoluteSourceEndpointIsNotKnown()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();
            
            var httpClientFactory = new Moq.Mock<IHttpClientFactory>().Object;

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = default(Uri);

            var wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory, sourceEndpoint, cancellationToken);

            Assert.AreSame(WopiDiscoveryDocument.Empty, wopiDiscoveryDocument, "Discovery document should be empty when the endpoint URL is not known");

            sourceEndpoint = new Uri("/relative/path", UriKind.Relative);

            wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory, sourceEndpoint, cancellationToken);

            Assert.AreSame(WopiDiscoveryDocument.Empty, wopiDiscoveryDocument, "Discovery document should be empty when the endpoint URL is not an absolute uri");
        }        

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task GetAsync_ThrowsWhenCancellationTokenCancelled()
        {
            using var cts = new CancellationTokenSource();

            cts.Cancel();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            var httpMessageHandler = new HttpMessageHandlerStub(
                (request, tkn) => {

                    cts.Cancel();

                    return httpResponseMessage;
                });

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cts.Token);
        }

        [TestMethod]
        public async Task GetAsync_UsesHttpClientFromFactoryAndConstructsExpectedRequest()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            var messageHandlerInvoked = false;

            var httpMessageHandler = new HttpMessageHandlerStub(
                (request, _) => {

                    messageHandlerInvoked = true;

                    Assert.AreSame(sourceEndpoint, request.RequestUri, "Expected the supplied endpoint to be used to retrieve the document");

                    Assert.IsTrue(request.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/xml")), "Expected the accept header to be set to retrieve an xml document");
                    Assert.IsTrue(request.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("text/xml")), "Expected the accept header to be set to retrieve an xml document");

                    return httpResponseMessage;
                    });

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            Assert.IsTrue(messageHandlerInvoked, "Expected the stubbed request message handler to have been invoked to retrieve the xml document from the WOPI client");
        }

        [TestMethod]
        public async Task GetAsync_ReturnsEmptyDocumentIfResponseContentTypeHeaderIsMissing()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            var wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            Assert.AreSame(WopiDiscoveryDocument.Empty, wopiDiscoveryDocument);
        }

        [TestMethod]
        public async Task GetAsync_ReturnsEmptyDocumentIfResponseContentTypeHeaderIsNotSupported()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            httpResponseMessage.Headers.Add("ContentType", "unsupported/contenttype");

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            var wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            Assert.AreSame(WopiDiscoveryDocument.Empty, wopiDiscoveryDocument);
        }

        [TestMethod]
        public async Task GetAsync_ReturnsPrimedDocumentForValidWopiClientXmlResponse()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(WOPI_DISCOVERY_DOCUMENT_XML, Encoding.UTF8, "application/xml")
            };

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            var wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            Assert.IsFalse(wopiDiscoveryDocument.IsEmpty);
            Assert.IsFalse(wopiDiscoveryDocument.IsTainted);
        }

        [TestMethod]
        public async Task GetAsync_ReturnsEmptyDocumentForInvalidWopiClientXmlResponse()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("<xml></xml>", Encoding.UTF8, "application/xml")
            };

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            var wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            Assert.IsTrue(wopiDiscoveryDocument.IsEmpty);
        }




        [TestMethod]
        public async Task GetEndpointForFileExtension_ReturnsCorrectEndpointForBothKnownAndUnknownFileExtensions()
        {
            var cancellationToken = new CancellationToken();

            var systemClock = new SystemClock();

            var logger = new Moq.Mock<ILogger>().Object;

            var sourceEndpoint = new Uri(WOPI_ROOT + "client/hosting/discovery", UriKind.Absolute);

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(WOPI_DISCOVERY_DOCUMENT_XML, Encoding.UTF8, "application/xml")
            };

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            httpClientFactory.Setup(x => x.CreateClient(Moq.It.IsAny<string>())).Returns(httpClient);

            IWopiDiscoveryDocument wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(systemClock, logger, httpClientFactory.Object, sourceEndpoint, cancellationToken);

            var endpoint = wopiDiscoveryDocument.GetEndpointForFileExtension("docx", "edit", new Uri(WOPI_SRC, UriKind.Absolute));

            Assert.IsNotNull(endpoint, "Expected the endpoint to be returned when it is supported by the wopi client");
            Assert.IsTrue(endpoint.IsAbsoluteUri, "The endpoint should be an absolute uri for supported file extensions");

            endpoint = wopiDiscoveryDocument.GetEndpointForFileExtension("fakefileextension", "edit", new Uri(WOPI_SRC, UriKind.Absolute));

            Assert.IsNull(endpoint, "Expected a null return value when the file extension is not supported by the wopi client");
        }

        [TestMethod]
        [ExpectedException(typeof(DocumentEmptyException))]
        public void GetEndpointForFileExtension_ThrowsIfDocumentIsEmpty()
        {
            IWopiDiscoveryDocument wopiDiscoveryDocument = WopiDiscoveryDocument.Empty;

            _ = wopiDiscoveryDocument.GetEndpointForFileExtension("docx", "edit", new Uri(WOPI_SRC, UriKind.Absolute));
        }


        
        [TestMethod]
        [ExpectedException(typeof(DocumentEmptyException))]
        public void IsProofInvalid_ThrowsIfDocumentIsEmpty()
        {
            var httpContext = new DefaultHttpContext();

            IWopiDiscoveryDocument wopiDiscoveryDocument = WopiDiscoveryDocument.Empty;

            _ = wopiDiscoveryDocument.IsProofInvalid(httpContext.Request);
        }



        // TODO - Acquire valid proof signed against the correct source URL so we can test against our fake discovery document, 
        // noting we will need to fake the system time to align with the unix timestamp used in the signing process
        public void IsProofInvalid_ProofsAreCorrectlyIdentifiedAsEitherVaildOrInvalid(
            string filename,
            string accessToken,
            string unixTimestamp, 
            string proof, 
            string oldProof
            )
        {
            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Headers["X-WOPI-Timestamp"] = unixTimestamp;
            httpRequest.Headers["X-WOPI-Proof"] = proof;
            httpRequest.Headers["X-WOPI-ProofOld"] = oldProof;

            httpRequest.Scheme = "https";
            httpRequest.Host = new HostString("futurenhs.cds.co.uk");
            httpRequest.Path = $"gateway/wopi/host/files/{filename}";
            httpRequest.QueryString = new QueryString($"?access_token={accessToken}");

        }
    }
}
