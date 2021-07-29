using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS.WOPIHost
{
    public interface IWopiDiscoveryDocumentFactory
    {
        Task<IWopiDiscoveryDocument> CreateDocumentAsync(CancellationToken cancellationToken);
    }

    internal sealed class WopiDiscoveryDocumentFactory
        : IWopiDiscoveryDocumentFactory
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;

        public WopiDiscoveryDocumentFactory(IMemoryCache memoryCache, IHttpClientFactory httpClientFactory)
        {
            _memoryCache = memoryCache;
            _httpClientFactory = httpClientFactory;
        }

        async Task<IWopiDiscoveryDocument> IWopiDiscoveryDocumentFactory.CreateDocumentAsync(CancellationToken cancellationToken)
        {
            _memoryCache.TryGetWopiDiscoveryDocument(out var wopiDiscoveryDocument);

            if (wopiDiscoveryDocument is null || wopiDiscoveryDocument.IsTainted)
            {
                var builder = new UriBuilder
                {
                    Scheme = "http",
                    Host = "127.0.0.1",
                    Port = 9980,
                    Path = Path.Combine("hosting", "discovery")
                };

                wopiDiscoveryDocument = await WopiDiscoveryDocument.GetAsync(_httpClientFactory, builder.Uri, cancellationToken);

                _memoryCache.TrySetWopiDiscoveryDocument(wopiDiscoveryDocument);

                return wopiDiscoveryDocument;
            }

            return wopiDiscoveryDocument;
        }

    }
}
