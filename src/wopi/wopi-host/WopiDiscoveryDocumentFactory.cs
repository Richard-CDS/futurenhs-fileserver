using FutureNHS.WOPIHost.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    public interface IWopiDiscoveryDocumentFactory
    {
        Task<IWopiDiscoveryDocument> CreateDocumentAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Tasked with obtaining a valid WOPI discovery document from the WOPI-Client
    /// </summary>
    /// <remarks>
    /// The discovery document contains important information with respect to what file types the client supports along 
    /// with details of the public part of a crytographic pair that the client will use to sign requests so we can be 
    /// sure they are coming from our trusted source and haven't been tampered with in transit.
    /// It is important to note that this pair of keys can be recycled and when this happens we need to refresh the 
    /// document direct from source to get the new details (the old keys stay alive for a short period)
    /// </remarks>
    internal sealed class WopiDiscoveryDocumentFactory
        : IWopiDiscoveryDocumentFactory
    {
        private readonly ISystemClock _systemClock;
        private readonly ILogger<WopiDiscoveryDocumentFactory> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WopiConfiguration _wopiConfiguration;

        public WopiDiscoveryDocumentFactory(ISystemClock systemClock, ILogger<WopiDiscoveryDocumentFactory> logger, IMemoryCache memoryCache, IHttpClientFactory httpClientFactory, IOptionsSnapshot<WopiConfiguration> wopiConfiguration)
        {
            _logger = logger                              ?? throw new ArgumentNullException(nameof(logger));
            _systemClock = systemClock                    ?? throw new ArgumentNullException(nameof(systemClock));
            _memoryCache = memoryCache                    ?? throw new ArgumentNullException(nameof(memoryCache));
            _httpClientFactory = httpClientFactory        ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _wopiConfiguration = wopiConfiguration?.Value ?? throw new ArgumentNullException(nameof(wopiConfiguration));
        }

        async Task<IWopiDiscoveryDocument> IWopiDiscoveryDocumentFactory.CreateDocumentAsync(CancellationToken cancellationToken)
        {
            _memoryCache.TryGetWopiDiscoveryDocument(out var wopiDiscoveryDocument);

            if (wopiDiscoveryDocument is null || wopiDiscoveryDocument.IsTainted)
            {
                var clientDiscoveryDocumentEndpoint = _wopiConfiguration.ClientDiscoveryDocumentEndpoint;

                if (!Uri.IsWellFormedUriString(clientDiscoveryDocumentEndpoint, UriKind.Absolute)) return default;

                var discoveryDocumentUrl = new Uri(clientDiscoveryDocumentEndpoint, UriKind.Absolute);

                wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(_systemClock, _logger, _httpClientFactory, discoveryDocumentUrl, cancellationToken);

                if (wopiDiscoveryDocument.IsEmpty) wopiDiscoveryDocument = default;

                _memoryCache.TrySetWopiDiscoveryDocument(wopiDiscoveryDocument);

                return wopiDiscoveryDocument;
            }

            return wopiDiscoveryDocument;
        }

    }
}
