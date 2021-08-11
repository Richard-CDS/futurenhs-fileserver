using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
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
        bool IsEmpty { get; }

        Uri GetEndpointForFileExtension(string fileExtension, string fileAction, Uri wopiHostFileEndpointUrl);

        bool IsProofInvalid(HttpRequest httpRequest);
    }

    public sealed class DocumentEmptyException : ApplicationException
    {
        public DocumentEmptyException() : base("The WOPI Discovery Document is Empty and cannot be used to perform the requested action") { }
    }

    internal sealed class WopiDiscoveryDocument
        : IWopiDiscoveryDocument
    {
        internal static readonly WopiDiscoveryDocument Empty = new WopiDiscoveryDocument();

        private readonly Uri _source;
        private readonly XDocument _xml;
        private readonly ISystemClock _systemClock;
        private readonly ILogger _logger;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

        private WopiDiscoveryDocument() { }

        private WopiDiscoveryDocument(Uri source, XDocument xml, ISystemClock systemClock, ILogger logger) 
        { 
            _source = source           ?? throw new ArgumentNullException(nameof(source));
            _xml = xml                 ?? throw new ArgumentNullException(nameof(xml));
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            _logger = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        internal static async Task<WopiDiscoveryDocument> GetAsync(ISystemClock systemClock, ILogger logger, IHttpClientFactory httpClientFactory, Uri sourceEndpoint, CancellationToken cancellationToken)
        {
            if (sourceEndpoint is null) return Empty;

            if (!sourceEndpoint.IsAbsoluteUri) return Empty;

            cancellationToken.ThrowIfCancellationRequested();

            using var client = httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, sourceEndpoint);

            var xmlMediaTypes = new[] { "application/xml", "text/xml" };

            var accepts = xmlMediaTypes.Aggregate(string.Empty, (acc, n) => string.Concat(acc, n, ", "))[0..^2];

            request.Headers.Add("Accept", accepts);

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode) return Empty;

            var contentType = response.Content?.Headers.ContentType.MediaType.Trim();

            if (string.IsNullOrWhiteSpace(contentType)) return Empty;

            if (!accepts.Contains(contentType, StringComparison.OrdinalIgnoreCase)) return Empty;

            using var strm = await response.Content.ReadAsStreamAsync();

            var xml = await XDocument.LoadAsync(strm, LoadOptions.None, cancellationToken);

            if (IsXmlDocumentSupported(xml)) return new WopiDiscoveryDocument(sourceEndpoint, xml, systemClock, logger);

            return Empty;
        }

        /// <summary>
        /// Responsible for validating that the schema of the discovery document returned by the WOPI client can 
        /// be successfully navigated by this host application
        /// </summary>
        /// <param name="xml">The xml document returned to us from the WOPI client</param>
        /// <returns>true if the document looks good, else false for an invalid document</returns>
        /// <remarks>
        /// TODO - Need to use an XML schema document to fully validate it
        /// </remarks>
        private static bool IsXmlDocumentSupported(XDocument xml)
        {
            if (xml is null) return false;

            var rootElement = xml.Element(XName.Get("wopi-discovery"));

            if (rootElement is null) return false;

            return true;
        }

        public bool IsTainted { get; private set; }
        public bool IsEmpty => ReferenceEquals(this, Empty);

        Uri IWopiDiscoveryDocument.GetEndpointForFileExtension(string fileExtension, string fileAction, Uri wopiHostFileEndpointUrl)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html

            if (IsEmpty) throw new DocumentEmptyException();

            if (string.IsNullOrWhiteSpace(fileExtension)) return default;

            if (fileExtension.StartsWith('.')) fileExtension = fileExtension.Substring(1);

            _ = _contentTypeProvider.TryGetContentType("." + fileExtension, out var fileContentType);

            if (wopiHostFileEndpointUrl is null) return default;

            if (!wopiHostFileEndpointUrl.IsAbsoluteUri) return default;

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

                        if (!string.Equals(name, fileAction, StringComparison.OrdinalIgnoreCase)) continue;
                    }

                    var urlSrc = actionElement.Attribute("urlsrc").Value;

                    urlSrc = TransformActionUrlSrcAttribute(urlSrc, wopiHostFileEndpointUrl);

                    return new Uri(string.Concat(urlSrc, "WOPISrc=", wopiHostFileEndpointUrl.AbsoluteUri), UriKind.Absolute);
                }
            }

            return default;
        }

#if DEBUG
        internal static
#else
        private 
#endif
        string TransformActionUrlSrcAttribute(string urlSrc, Uri wopiHostFileEndpointUrl)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html#transforming-the-urlsrc-parameter

            // MANDATORY - If the urlSrc contains a placeholder for the wopiSrc then we must replace with the correct value


            while (urlSrc.Contains("<WOPI_SRC=PLACEHOLDER_VALUE"))
            {
                urlSrc = urlSrc.Replace("<WOPI_SRC=PLACEHOLDER_VALUE[&]>", $"WOPI_SRC={wopiHostFileEndpointUrl.AbsoluteUri}&", StringComparison.Ordinal);
                urlSrc = urlSrc.Replace("<WOPI_SRC=PLACEHOLDER_VALUE>",    $"WOPI_SRC={wopiHostFileEndpointUrl.AbsoluteUri}",  StringComparison.Ordinal);
            }

            // MANDATORY - At this point, we have replaced all the placeholder parameters that we know about, therefore the 
            // remaining ones need to be removed so the WOPI client can use their default value

            int n;

            while (0 <= (n = urlSrc.IndexOf("<")))
            {
                var i = urlSrc.IndexOf("=PLACEHOLDER_VALUE[&]>", n, StringComparison.Ordinal);

                if (0 >= i)
                {
                    i = urlSrc.IndexOf("=PLACEHOLDER_VALUE>", n, StringComparison.Ordinal);

                    urlSrc = urlSrc.Substring(0, n) + urlSrc.Substring(i + "=PLACEHOLDER_VALUE>".Length);
                }
                else if (0 == n)
                {
                    urlSrc = urlSrc.Substring(0, "=PLACEHOLDER_VALUE[&]>".Length);
                }
                else
                { 
                    urlSrc = urlSrc.Substring(0, n) + urlSrc.Substring(i + "=PLACEHOLDER_VALUE[&]>".Length);
                }
            }

            // If the string ends with an errant ? or & character, remove it

            if (urlSrc.EndsWith('?')) urlSrc = urlSrc[0..^1];
            else if (urlSrc.EndsWith('&')) urlSrc = urlSrc[0..^1];

            return urlSrc;
        }


        bool IWopiDiscoveryDocument.IsProofInvalid(HttpRequest request)
        {
            // https://wopi.readthedocs.io/en/latest/scenarios/proofkeys.html

            if (IsEmpty) throw new DocumentEmptyException();

            // Validate WOPI ProofKey to make sure request came from the expected server.

            const bool PROOF_IS_INVALID = true;
            const bool PROOF_IS_VALID = false;

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
            var oldKey = proofKey.Attribute(XName.Get("oldvalue")).Value;

            var givenProof = request.Headers["X-WOPI-Proof"].Single().Trim();
            var oldGivenProof = request.Headers["X-WOPI-ProofOld"].Single()?.Trim();

            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: access_token = {0}", accessToken);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: proof-key.value = {0}", key);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: proof-key.oldvalue = {0}", oldKey);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-Timestamp = {0}", timestamp);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-Proof = {0}", givenProof);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-ProofOld = {0}", oldGivenProof);

            // Verify if the given proof matches what we would expect to see if it has been signed by the current key

            if (ProofHasBeenVerifiedToBeValid(expectedProof, givenProof, key)) return PROOF_IS_VALID;

            // It may be that the proof was signed by a key newer than what we know of.  In this case we should see 
            // another proof signed by the key that is now an old key, but that we still consider to be current

            if (string.IsNullOrWhiteSpace(oldGivenProof)) return PROOF_IS_VALID;

            // If the old proof matches with our current key we need to refresh the discovery document
            // because that is a clear signal there is a new one waiting to be downloaded

            if (ProofHasBeenVerifiedToBeValid(expectedProof, oldGivenProof, key)) return PROOF_IS_VALID == (IsTainted = true);

            // Hmmm, the last valid scenario is that the given proof was signed by the older key, which could 
            // happen if we recently acquired the discovery document, but the proof was generated before the keys
            // were rotated.  We'll have a max 20 minutes check on the timestamp to ensure it doesn't linger 
            // around for longer than we're comfortable with

            var currentTimestamp = _systemClock.UtcNow.Ticks;

            if ((currentTimestamp - timestamp) > TimeSpan.FromMinutes(20).Ticks) return PROOF_IS_INVALID;

            return !ProofHasBeenVerifiedToBeValid(expectedProof, givenProof, oldKey);
        }

        private bool ProofHasBeenVerifiedToBeValid(byte[] expectedProof, string signedProof, string publicKeyCspBlob)
        {
            const bool HAS_NOT_BEEN_VERIFIED = false;

            var publicKey = Convert.FromBase64String(publicKeyCspBlob);

            var proof = Convert.FromBase64String(signedProof);

            try
            {
                using var cryptoProvider = new RSACryptoServiceProvider();

                cryptoProvider.ImportCspBlob(publicKey);

                return cryptoProvider.VerifyData(expectedProof, "SHA256", proof);
            }
            catch (FormatException) { return HAS_NOT_BEEN_VERIFIED; }
            catch (CryptographicException) { return HAS_NOT_BEEN_VERIFIED; }
        }
    }
}
