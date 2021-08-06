using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FutureNHS.WOPIHost
{
    public interface IWopiDiscoveryDocument
    {
        bool IsTainted { get; } 

        Task<string> GetEndpointForFileExtensionAsync(string fileExtension, string action, Uri wopiSrc);

        Task<bool> IsProofKeyInvalidAsync(HttpRequest request);
    }

    internal sealed class WopiDiscoveryDocument
        : IWopiDiscoveryDocument
    {
        private readonly Uri _source;
        private readonly XDocument _xml;
        private readonly Func<DateTimeOffset> _getUtcNow;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

        private WopiDiscoveryDocument(Uri source, XDocument xml, Func<DateTimeOffset> getUtcNow) 
        { 
            _source = source;
            _xml = xml; 
            _getUtcNow = getUtcNow;
        }

        internal static async Task<WopiDiscoveryDocument> GetAsync(IHttpClientFactory httpClientFactory, Uri uri, Uri proxyPrefix, CancellationToken cancellationToken)
        {
            if (uri is null) return default;

            if (!uri.IsAbsoluteUri) return default;

            using var client = httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var xmlMediaTypes = new[] { "application/xml", "text/xml" };

            var accepts = xmlMediaTypes.Aggregate(string.Empty, (acc, n) => string.Concat(acc, n, ", "))[0..^2];

            request.Headers.Add("Accept", accepts);

            if (proxyPrefix is object) request.Headers.Add("ProxyPrefix", proxyPrefix.AbsoluteUri);

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) return default;

            if (!accepts.Contains(response.Content.Headers.ContentType.MediaType.Trim(), StringComparison.OrdinalIgnoreCase)) return default;

            using var strm = await response.Content.ReadAsStreamAsync();

            var xDoc = await XDocument.LoadAsync(strm, LoadOptions.None, cancellationToken);

            return new WopiDiscoveryDocument(uri, xDoc, () => DateTimeOffset.UtcNow);
        }

        public bool IsTainted { get; private set; }

        async Task<string> IWopiDiscoveryDocument.GetEndpointForFileExtensionAsync(string fileExtension, string action, Uri wopiSrc)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html

            if (string.IsNullOrWhiteSpace(fileExtension)) return default;

            if (fileExtension.StartsWith('.')) fileExtension = fileExtension?.Substring(1);

            _ = _contentTypeProvider.TryGetContentType("." + fileExtension, out var fileContentType);

            if (wopiSrc is null) return default;

            if (!wopiSrc.IsAbsoluteUri) return default;

            var rootElement = _xml.Element(XName.Get("wopi-discovery"));

            var netZoneElement = rootElement.Element(XName.Get("net-zone"));

            foreach (var appElement in netZoneElement.Elements("app"))
            {
                var appName = appElement.Attribute("name").Value;

                foreach (var actionElement in appElement.Elements("action"))
                {
                    if (!string.Equals(appName, fileContentType))
                    {
                        var ext = actionElement.Attribute("ext").Value;

                        if (!string.Equals(ext, fileExtension, StringComparison.OrdinalIgnoreCase)) continue;

                        var name = actionElement.Attribute("name").Value; // https://wopi.readthedocs.io/en/latest/discovery.html#wopi-actions

                        if (!string.Equals(name, action, StringComparison.OrdinalIgnoreCase)) continue;
                    }

                    var urlSrc = actionElement.Attribute("urlsrc").Value;

                    urlSrc = TransformActionUrlSrcAttribute(urlSrc);

                    return string.Concat(urlSrc, "WOPISrc=", wopiSrc.AbsoluteUri);
                }
            }

            return string.Empty;
        }

        private static string TransformActionUrlSrcAttribute(string urlSrc)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html#transforming-the-urlsrc-parameter

            return urlSrc;
        }

        Task<bool> IWopiDiscoveryDocument.IsProofKeyInvalidAsync(HttpRequest request)
        {
            // https://wopi.readthedocs.io/en/latest/scenarios/proofkeys.html

            // Validate WOPI ProofKey to make sure request came from the expected server.

            var accessToken = request.Query["access_token"].Single().Trim();

            var accessTokenBytes = Encoding.UTF8.GetBytes(accessToken);

            var wopiRequestUrl = UriHelper.GetEncodedUrl(request).Trim().ToUpperInvariant();

            var wopiRequestUrlBytes = Encoding.UTF8.GetBytes(wopiRequestUrl);

            var timestamp = Convert.ToInt64(request.Headers["X-WOPI-Timestamp"].Single().Trim());

            var timestampBytes = BitConverter.GetBytes(timestamp).Reverse().ToArray();

            var proof = new List<byte>(4 + accessTokenBytes.Length + 4 + wopiRequestUrlBytes.Length + 4 + timestampBytes.Length);

            proof.AddRange(BitConverter.GetBytes(accessTokenBytes.Length).Reverse());
            proof.AddRange(accessTokenBytes);
            proof.AddRange(BitConverter.GetBytes(wopiRequestUrlBytes.Length).Reverse());
            proof.AddRange(wopiRequestUrlBytes);
            proof.AddRange(BitConverter.GetBytes(timestampBytes.Length).Reverse());
            proof.AddRange(timestampBytes);

            var expectedProof = proof.ToArray();

            // Extract the proof keys from the discovery document, noting there are two to consider in the event of a key
            // rotation

            var root = _xml.Element(XName.Get("wopi-discovery"));

            var proofKey = root.Element(XName.Get("proof-key"));

            var key = proofKey.Attribute(XName.Get("value")).Value;

            var givenProof = request.Headers["X-WOPI-Proof"].Single().Trim();

            // Verify if the given proof matches what we would expect to see, if it has been signed by the current key

            if (HasBeenVerified(expectedProof, givenProof, key)) return Task.FromResult(false);

            // It may be that the proof was signed by a key newer that what we know of.  In this case we should see 
            // another proof signed by the key that is now an old key, but that we still consider to be current

            var oldGivenProof = request.Headers["X-WOPI-ProofOld"].Single()?.Trim();

            if (string.IsNullOrWhiteSpace(oldGivenProof)) return Task.FromResult(false);

            var oldKey = proofKey.Attribute(XName.Get("oldvalue")).Value;

            // If the old proof matches with our current key we need to refresh the discovery document
            // because that is a clear signal there is a new one waiting to be downloaded

            if (HasBeenVerified(expectedProof, oldGivenProof, key)) return Task.FromResult(false == (IsTainted = true));

            // Hmmm, the last valid scenario is that the given proof was signed by the older key, which could 
            // happen if we recently acquired the discovery document, but the proof was generated before the keys
            // were rotated.  We'll have a max 20 minutes check on the timestamp to ensure it doesn't linger 
            // around for longer than we're comfortable with

            var currentTimestamp = _getUtcNow().Ticks;

            if ((currentTimestamp - timestamp) > TimeSpan.FromMinutes(20).Ticks) return Task.FromResult(true);

            return Task.FromResult(!HasBeenVerified(expectedProof, givenProof, oldKey));
        }

        private bool HasBeenVerified(byte[] expectedProof, string signedProof, string publicKeyCspBlob)
        {
            var publicKey = Convert.FromBase64String(publicKeyCspBlob);

            var proof = Convert.FromBase64String(signedProof);

            try
            {
                using var cryptoProvider = new RSACryptoServiceProvider();

                cryptoProvider.ImportCspBlob(publicKey);

                return cryptoProvider.VerifyData(expectedProof, "SHA256", proof);
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }
    }
}
