using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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

        private readonly Uri _sourceEndpoint;
        private readonly XDocument _xml;
        private readonly ILogger _logger;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

        private readonly string _publicKeyCspBlob;
        private readonly string _oldPublicKeyCspBlob;

        private WopiDiscoveryDocument() { }

        private WopiDiscoveryDocument(Uri sourceEndpoint, XDocument xml, ILogger logger) 
        { 
            _sourceEndpoint = sourceEndpoint ?? throw new ArgumentNullException(nameof(sourceEndpoint));
            _xml = xml                       ?? throw new ArgumentNullException(nameof(xml));
            _logger = logger                 ?? throw new ArgumentNullException(nameof(logger));

            // Extract the proof keys from the discovery document, noting there may be two to consider in the event of a key
            // rotation

            var root = _xml.Element(XName.Get("wopi-discovery"));

            var proofKey = root.Element(XName.Get("proof-key"));

            _publicKeyCspBlob = proofKey.Attribute(XName.Get("value")).Value;
            _oldPublicKeyCspBlob = proofKey.Attribute(XName.Get("oldvalue")).Value;
        }

        /// <summary>
        /// Constructor, responsible for trying to obtain the WOPI discovery document for the trusted WOPI client and checking to ensure it is 
        /// in a form that is consumable by this application
        /// </summary>
        /// <param name="httpClient">The client to be used for requesting the discovery document from the remote WOPI client</param>
        /// <param name="sourceEndpoint">The URL of the http endpoint from which the disovery document can be downloaded from the trusted WOPI client</param>
        /// <param name="logger">An optional logger to which pertinent debug information can be written</param>
        /// <param name="cancellationToken">A token that can be used to identify if our long running process should be terminated eagerly</param>
        /// <returns>
        /// Either <see cref="WopiDiscoveryDocument.Empty"/> to represent a 'failed' attempt to pull/parse the discovery document, or an 
        /// appropriately configured instance ready to receive discovery requests
        /// </returns>
        internal static async Task<WopiDiscoveryDocument> GetAsync(HttpClient httpClient, Uri sourceEndpoint, ILogger logger, CancellationToken cancellationToken)
        {
            if (sourceEndpoint is null) return Empty;

            if (!sourceEndpoint.IsAbsoluteUri) return Empty;

            if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));

            cancellationToken.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Get, sourceEndpoint);

            var xmlMediaTypes = new[] { "application/xml", "text/xml" };

            var accepts = xmlMediaTypes.Aggregate(string.Empty, (acc, n) => string.Concat(acc, n, ", "))[0..^2];

            request.Headers.Add("Accept", accepts);

            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode) return Empty;

                var contentType = response.Content?.Headers.ContentType.MediaType.Trim();

                if (string.IsNullOrWhiteSpace(contentType)) return Empty;

                if (!accepts.Contains(contentType, StringComparison.OrdinalIgnoreCase)) return Empty;

                using var strm = await response.Content.ReadAsStreamAsync();

                var xml = await XDocument.LoadAsync(strm, LoadOptions.None, cancellationToken);

                if (IsXmlDocumentSupported(xml)) return new WopiDiscoveryDocument(sourceEndpoint, xml, logger);
            }
            catch (HttpRequestException ex) { logger.LogError(ex, "Failed to connect to the WOPI Client to download the discovery document"); }

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

            // TODO - Ensure there is a proof key section in the document

            return true;
        }

        /// <summary>
        /// Whether this document has identified itself as out-of-date and thus in need of being refetched from the 
        /// source WOPI client.  Only accurate if <see cref="IsEmpty"/> = false.
        /// </summary>
        public bool IsTainted { get; private set; }

        /// <summary>
        /// Whether this instance of <see cref="WopiDiscoveryDocument"/> represent an 'Empty' container state (ie not a fully formed
        /// document) or not
        /// </summary>
        public bool IsEmpty => ReferenceEquals(this, Empty);

        Uri IWopiDiscoveryDocument.GetEndpointForFileExtension(string fileExtension, string fileAction, Uri wopiHostFileEndpointUrl)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html

            if (IsEmpty) throw new DocumentEmptyException();

            if (string.IsNullOrWhiteSpace(fileExtension)) return default;
            if (string.IsNullOrWhiteSpace(fileAction)) return default;

            if (wopiHostFileEndpointUrl is null) return default;
            if (!wopiHostFileEndpointUrl.IsAbsoluteUri) return default;

            if (fileExtension.StartsWith('.')) fileExtension = fileExtension.Substring(1);

            _ = _contentTypeProvider.TryGetContentType("." + fileExtension, out var fileContentType);

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

        /// <summary>
        /// Responsible for assuring the url attribute of an action element in the discovery document is correctly formed.
        /// This process mainly involves the replacement of placeholder values (that could not be determined by the WOPI client)
        /// with the real one that we are responsible for maintaining
        /// </summary>
        /// <param name="urlSrc"></param>
        /// <param name="wopiHostFileEndpointUrl"></param>
        /// <returns></returns>
#if DEBUG
        internal static // for local testing of private method
#else
        private 
#endif
        string TransformActionUrlSrcAttribute(string urlSrc, Uri wopiHostFileEndpointUrl)
        {
            // https://wopi.readthedocs.io/en/latest/discovery.html#transforming-the-urlsrc-parameter

            // If the urlSrc contains a placeholder for the wopiSrc then we must replace with the correct value

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

            // If the string ends with an errant & character, remove it
            // Important to note that the string must end with a ? character for things to work correctly when building the 
            // loleaflet endpoint (undocumented feature!)

            if (urlSrc.EndsWith('&')) urlSrc = urlSrc[0..^1];

            return urlSrc;
        }


        /// <summary>
        /// Validate WOPI ProofKey to make sure request came from a WOPI client that we trust.
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://wopi.readthedocs.io/en/latest/scenarios/proofkeys.html 
        /// </remarks>
        bool IWopiDiscoveryDocument.IsProofInvalid(HttpRequest httpRequest)
        {
            if (IsEmpty) throw new DocumentEmptyException();

            if (httpRequest is null) throw new ArgumentNullException(nameof(httpRequest));

            const bool PROOF_IS_INVALID = true;
            const bool PROOF_IS_VALID = false;
                        
            var encodedUrl = new Uri(httpRequest.GetEncodedUrl(), UriKind.Absolute);

            var accessToken = HttpUtility.ParseQueryString(encodedUrl.Query)["access_token"];
            
            var encodedAccessToken = HttpUtility.UrlEncode(accessToken);

            var encodedRequestUrl = httpRequest.GetEncodedUrl();

            var wopiHostUrl = encodedRequestUrl.ToUpperInvariant();

            var timestamp = Convert.ToInt64(httpRequest.Headers["X-WOPI-Timestamp"].Single().Trim());

            var accessTokenBytes = Encoding.UTF8.GetBytes(encodedAccessToken);
            var wopiHostUrlBytes = Encoding.UTF8.GetBytes(wopiHostUrl);
            var timestampBytes = BitConverter.GetBytes(timestamp).Reverse().ToArray();

            var proof = new List<byte>(4 + accessTokenBytes.Length + 4 + wopiHostUrlBytes.Length + 4 + timestampBytes.Length);

            proof.AddRange(BitConverter.GetBytes(accessTokenBytes.Length).Reverse());
            proof.AddRange(accessTokenBytes);
            proof.AddRange(BitConverter.GetBytes(wopiHostUrlBytes.Length).Reverse());
            proof.AddRange(wopiHostUrlBytes);
            proof.AddRange(BitConverter.GetBytes(timestampBytes.Length).Reverse());
            proof.AddRange(timestampBytes);

            var expectedProof = proof.ToArray();

            var givenProof = httpRequest.Headers["X-WOPI-Proof"].Single().Trim();
            var oldGivenProof = httpRequest.Headers["X-WOPI-ProofOld"].Single()?.Trim();

            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: request_url = {0}", encodedRequestUrl);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: access_token = {0}", encodedAccessToken);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: proof-key.value = {0}", _publicKeyCspBlob);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: proof-key.oldvalue = {0}", _oldPublicKeyCspBlob);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-Timestamp = {0}", timestamp);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-Proof = {0}", givenProof);
            _logger?.LogDebug("WopiDiscoveryDocument-PROOF_CHECK: X-WOPI-ProofOld = {0}", oldGivenProof);

            // Is the proof verifiable using either our current key or the old one?  If not, maybe there is a new key that we 
            // do not know about, thus we might be able to verify using the old proof with our current key (ie our current key is old
            // but we are still working with a now outdated discovery document which we need to refresh).

            if (IsProven(expectedProof, givenProof, _publicKeyCspBlob)) return PROOF_IS_VALID;                              // discovery doc is the latest
            if (IsProven(expectedProof, oldGivenProof, _publicKeyCspBlob)) return PROOF_IS_VALID == (IsTainted = true);     // discovery doc needs to be refreshed

            // Next scenario is one where our discovery document is up to date, but the proof was generated using an old key and if 
            // that doesn't work then using the old key to sign the old proof but having the new key fail to validate the new proof
            // smacks of dodgy shenanigans so I guess we'll just let that one fail

            if (IsProven(expectedProof, givenProof, _oldPublicKeyCspBlob)) return PROOF_IS_VALID;

            // There is a scenario that is impossible for us to distinguish from a potential attack, and that is the one where 
            // the WOPI client has rotated the keys mutliple times since we last refreshed the discovery document.  CODE generates 
            // random keys each time a container is created (which is something we will address to enable clustering) so has a similar
            // 'problem' when running locally.  Safest thing for us to do is to refetch the document whenever somethinbg fails validation
            // and mitigate the DDoS vector this opens up at the infrastructure level.

            IsTainted = true;

            return PROOF_IS_INVALID;
        }

        /// <summary>
        /// Tasked with verifying that the presented proof does indeed match that which we would have expected the 
        /// trusted WOPI client (from which we pulled the Discovery Document we represent) to have produced for the 
        /// specific request that has been made to us (the WOPI Host).  This is the approach taken to ensure the request
        /// validity is non-repudiable
        /// </summary>
        /// <param name="expectedProof">The proof we would have expected the trusted client to offer</param>
        /// <param name="signedProof">The proof presented to ourselves that needs to be verified</param>
        /// <param name="publicKeyCspBlob">The CSP Blob the trusted WOPI client is thought to have used to sign the proof source</param>
        /// <returns></returns>
        private bool IsProven(byte[] expectedProof, string signedProof, string publicKeyCspBlob)
        {
            Debug.Assert(expectedProof is object && 0 < expectedProof.Length);
            Debug.Assert(!string.IsNullOrWhiteSpace(signedProof));
            Debug.Assert(!string.IsNullOrWhiteSpace(publicKeyCspBlob));

            const bool HAS_NOT_BEEN_VERIFIED = false;

            const string SHA256 = "SHA256";

            var publicKey = Convert.FromBase64String(publicKeyCspBlob);

            var proof = Convert.FromBase64String(signedProof);

            try
            {
                using var rsaAlgorithm = new RSACryptoServiceProvider();

                rsaAlgorithm.ImportCspBlob(publicKey);

                return rsaAlgorithm.VerifyData(expectedProof, SHA256, proof);
            }
            catch (FormatException) { return HAS_NOT_BEEN_VERIFIED; }
            catch (CryptographicException) { return HAS_NOT_BEEN_VERIFIED; }
        }
    }
}
