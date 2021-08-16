using FutureNHS.WOPIHost;
using FutureNHS.WOPIHost.Configuration;
using FutureNHS_WOPI_Host_UnitTests.Stubs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class WopiDiscoveryDocumentFactoryTests
    {
        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task CreateDocumentAsync_ThrowsWhenCancellationTokenCancelled()
        {
            using var cts = new CancellationTokenSource();

            cts.Cancel();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object; 
            
            var memoryCache = new Moq.Mock<IMemoryCache>().Object;

            var wopiConfiguration = new WopiConfiguration();

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            _ = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cts.Token);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateDocumentAsync_ThrowsWhenConfigurationIsMissing()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var memoryCache = new Moq.Mock<IMemoryCache>().Object;

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            _ = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);
        }



        [TestMethod]
        public async Task CreateDocumentAsync_PullsDocumentFromCacheWhenOneIsAvailable()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var services = new ServiceCollection();

            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            var cachedWopiDiscoveryDocument = new Moq.Mock<IWopiDiscoveryDocument>();

            cachedWopiDiscoveryDocument.SetupGet(x => x.IsTainted).Returns(false);

            memoryCache.Set(ExtensionMethods.WOPI_DISCOVERY_DOCUMENT_CACHE_KEY, cachedWopiDiscoveryDocument.Object);

            var wopiConfiguration = new WopiConfiguration();

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>().Object;

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory, wopiConfigurationOptionsSnapshot.Object, logger);

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            Assert.AreSame(cachedWopiDiscoveryDocument.Object, wopiDiscoveryDocument, "Expected the cached document to have been returned");
        }

        [TestMethod]
        public async Task CreateDocumentAsync_DoesNotPullDocumentFromCacheWhenOneIsAvailableButIsMarkedAsTainted()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var taintedWopiDiscoveryDocument = new Moq.Mock<IWopiDiscoveryDocument>();

            taintedWopiDiscoveryDocument.SetupGet(x => x.IsTainted).Returns(true);

            var services = new ServiceCollection();

            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            memoryCache.Set(ExtensionMethods.WOPI_DISCOVERY_DOCUMENT_CACHE_KEY, taintedWopiDiscoveryDocument);

            var wopiConfiguration = new WopiConfiguration();

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            Assert.AreNotSame(taintedWopiDiscoveryDocument, wopiDiscoveryDocument, "Expected the tainted document to have been discarded and a new one generated");
        }

        [TestMethod]
        public async Task CreateDocumentAsync_ReturnsEmptyDocumentWhenDiscoveryEndpointIsNotKnown()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var services = new ServiceCollection();

            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            var wopiConfiguration = new WopiConfiguration() { ClientDiscoveryDocumentEndpoint = default };

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            Assert.IsTrue(wopiDiscoveryDocument.IsEmpty, "Expected an empty document to be returned when the discovery endpoint in not known");
        }

        [TestMethod]
        public async Task CreateDocumentAsync_ReturnsEmptyDocumentWhenDiscoveryEndpointIsNotAnAbsoluteUrl()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var services = new ServiceCollection();

            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            var wopiConfiguration = new WopiConfiguration() { ClientDiscoveryDocumentEndpoint = "relative/url" };

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            Assert.IsTrue(wopiDiscoveryDocument.IsEmpty, "Expected an empty document to be returned when the discovery endpoint in not an absolute url");
        }

        [TestMethod]
        public async Task CreateDocumentAsync_ReturnsPrimedDocumentWhenDiscoveryEndpointReturnsValidResponse()
        {
            var cancellationToken = new CancellationToken();

            var logger = new Moq.Mock<ILogger<WopiDiscoveryDocumentFactory>>().Object;

            var services = new ServiceCollection();

            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            var sourceEndpoint = new Uri(WopiDiscoveryDocumentTests.WOPI_ROOT + WopiDiscoveryDocumentTests.WOPI_DISCOVERY_DOCUMENT_URL, UriKind.Absolute);

            var wopiConfiguration = new WopiConfiguration() { ClientDiscoveryDocumentEndpoint = sourceEndpoint.AbsoluteUri };

            var wopiConfigurationOptionsSnapshot = new Moq.Mock<IOptionsSnapshot<WopiConfiguration>>();

            wopiConfigurationOptionsSnapshot.SetupGet(x => x.Value).Returns(wopiConfiguration);

            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();

            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(WopiDiscoveryDocumentTests.WOPI_DISCOVERY_DOCUMENT_XML, Encoding.UTF8, "application/xml")
            };

            var httpMessageHandler = new HttpMessageHandlerStub((request, _) => httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandler, true);

            httpClientFactory.Setup(x => x.CreateClient("wopi-discovery-document")).Returns(httpClient);

            IWopiDiscoveryDocumentFactory wopiDiscoveryDocumentFactory = new WopiDiscoveryDocumentFactory(memoryCache, httpClientFactory.Object, wopiConfigurationOptionsSnapshot.Object, logger);

            var wopiDiscoveryDocument = await wopiDiscoveryDocumentFactory.CreateDocumentAsync(cancellationToken);

            Assert.IsFalse(wopiDiscoveryDocument.IsEmpty, "Expected a none empty document to be returned when the discovery endpoint is known and returns a valid response");
        }
    }
}
