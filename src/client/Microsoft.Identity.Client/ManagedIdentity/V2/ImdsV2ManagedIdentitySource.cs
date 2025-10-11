// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.ManagedIdentity.V2.ImdsV2ManagedIdentitySource;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Provides authentication capabilities for Azure Managed Identities using the IMDSv2 protocol.
    /// This implementation handles certificate-based authentication flows, including certificate
    /// management, CSR (Certificate Signing Request) handling, and mTLS communication with Azure AD.
    /// </summary>
    /// <remarks>
    /// The IMDSv2 authentication flow consists of several steps:
    /// 1. Probing/retrieving metadata from the IMDS endpoint to verify availability
    /// 2. Creating or retrieving certificates for mTLS authentication
    /// 3. Requesting tokens using the appropriate certificate
    /// 
    /// For security and performance, this implementation:
    /// - Uses certificate caching and reuse when possible
    /// - Handles different token types (Bearer and PoP)
    /// - Supports attestation for KeyGuard-protected keys
    /// - Maintains separate certificate mappings per identity and token type
    /// 
    /// This class interacts with:
    /// - MsiCertManager: Handles certificate lifecycle operations
    /// - MtlsBindingStore: Manages certificate persistence in the system store
    /// - BindingMetadataPersistence: Provides storage of identity-to-certificate mappings
    /// </remarks>
    internal partial class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
    {
        // used in unit tests
        public const string ImdsV2ApiVersion = "2.0";
        public const string CsrMetadataPath = "/metadata/identity/getplatformmetadata";
        public const string CertificateRequestPath = "/metadata/identity/issuecredential";
        public const string AcquireEntraTokenPath = "/oauth2/v2.0/token";

        /// <summary>
        /// Retrieves CSR (Certificate Signing Request) metadata from the IMDS endpoint.
        /// This metadata is required to properly generate certificates for managed identity authentication.
        /// </summary>
        /// <param name="requestContext">Context for the current request, including logging</param>
        /// <param name="probeMode">When true, failures are treated as availability signals rather than errors</param>
        /// <returns>CSR metadata if available, or null if unavailable or in probe mode with failures</returns>
        public static async Task<CsrMetadata> GetCsrMetadataAsync(
            RequestContext requestContext,
            bool probeMode)
        {
#if NET462
            requestContext.Logger.Info("[Managed Identity] IMDSv2 flow is not supported on .NET Framework 4.6.2. Cryptographic operations required for managed identity authentication are unavailable on this platform. Skipping IMDSv2 probe.");
            return await Task.FromResult<CsrMetadata>(null).ConfigureAwait(false);
#else

            var queryParams = ImdsV2QueryParamsHelper(requestContext);

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { OAuth2Header.XMsCorrelationId, requestContext.CorrelationId.ToString() }
            };

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.CsrMetadataProbe);

            HttpResponse response = null;

            try
            {
                response = await requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                    ImdsManagedIdentitySource.GetValidatedEndpoint(requestContext.Logger, CsrMetadataPath, queryParams),
                    headers,
                    body: null,
                    method: HttpMethod.Get,
                    logger: requestContext.Logger,
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: requestContext.UserCancellationToken,
                    retryPolicy: retryPolicy)
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (probeMode)
                {
                    requestContext.Logger.Info($"[Managed Identity] IMDSv2 CSR endpoint failure. Exception occurred while sending request to CSR metadata endpoint: {ex}");
                    return null;
                }
                else
                {
                    ThrowProbeFailedException(
                        "ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed.",
                        ex);
                }
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (probeMode)
                {
                    requestContext.Logger.Info(() => $"[Managed Identity] IMDSv2 managed identity is not available. Status code: {response.StatusCode}, Body: {response.Body}");
                    return null;
                }
                else
                {
                    ThrowProbeFailedException(
                        $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed due to HTTP error. Status code: {response.StatusCode} Body: {response.Body}",
                        null,
                        (int)response.StatusCode);
                }
            }

            if (!ValidateCsrMetadataResponse(response, requestContext.Logger, probeMode))
            {
                return null;
            }

            return TryCreateCsrMetadata(response, requestContext.Logger, probeMode);
#endif
        }

        /// <summary>
        /// Creates a properly formatted exception for metadata probe failures.
        /// </summary>
        private static void ThrowProbeFailedException(
            String errorMessage,
            Exception ex = null,
            int? statusCode = null)
        {
            throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                MsalError.ManagedIdentityRequestFailed,
                $"[ImdsV2] {errorMessage}",
                ex,
                ManagedIdentitySource.ImdsV2,
                statusCode);
        }

        /// <summary>
        /// Validates the CSR metadata response from IMDS, checking for required headers and format.
        /// </summary>
        /// <returns>True if the response is valid, false if invalid in probe mode (throws otherwise)</returns>
        private static bool ValidateCsrMetadataResponse(
            HttpResponse response,
            ILoggerAdapter logger,
            bool probeMode)
        {
            string serverHeader = response.HeadersAsDictionary
                .FirstOrDefault((kvp) => {
                    return string.Equals(kvp.Key, "server", StringComparison.OrdinalIgnoreCase);
                }).Value;

            if (serverHeader == null)
            {
                if (probeMode)
                {
                    logger.Info(() => $"[Managed Identity] IMDSv2 managed identity is not available. 'server' header is missing from the CSR metadata response. Body: {response.Body}");
                    return false;
                }
                else
                {
                    ThrowProbeFailedException(
                        $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because response doesn't have server header. Status code: {response.StatusCode} Body: {response.Body}",
                        null,
                        (int)response.StatusCode);
                }
            }

            if (!serverHeader.Contains("IMDS", StringComparison.OrdinalIgnoreCase))
            {
                if (probeMode)
                {
                    logger.Info(() => $"[Managed Identity] IMDSv2 managed identity is not available. The 'server' header format is invalid. Extracted server header: {serverHeader}");
                    return false;
                }
                else
                {
                    ThrowProbeFailedException(
                        $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because the 'server' header format is invalid. Extracted server header: {serverHeader}. Status code: {response.StatusCode} Body: {response.Body}",
                        null,
                        (int)response.StatusCode);
                }
            }

            return true;
        }

        /// <summary>
        /// Parses and validates the CSR metadata from an HTTP response.
        /// </summary>
        /// <returns>A parsed CsrMetadata object</returns>
        private static CsrMetadata TryCreateCsrMetadata(
            HttpResponse response,
            ILoggerAdapter logger,
            bool probeMode)
        {
            CsrMetadata csrMetadata = JsonHelper.DeserializeFromJson<CsrMetadata>(response.Body);
            if (!CsrMetadata.ValidateCsrMetadata(csrMetadata))
            {
                ThrowProbeFailedException(
                    $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because the CsrMetadata response is invalid. Status code: {response.StatusCode} Body: {response.Body}",
                    null,
                    (int)response.StatusCode);
            }

            logger.Info(() => "[Managed Identity] IMDSv2 managed identity is available.");
            return csrMetadata;
        }

        /// <summary>
        /// Factory method to create a new instance of the IMDSv2 managed identity source.
        /// </summary>
        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            return new ImdsV2ManagedIdentitySource(requestContext);
        }

        /// <summary>
        /// Initializes a new instance of the IMDSv2 managed identity source.
        /// </summary>
        internal ImdsV2ManagedIdentitySource(RequestContext requestContext) :
            base(requestContext, ManagedIdentitySource.ImdsV2)
        { }

        /// <summary>
        /// Requests a certificate from the IMDS endpoint using a CSR.
        /// For KeyGuard-backed keys, includes attestation token in the request.
        /// </summary>
        /// <param name="clientId">Client ID of the managed identity</param>
        /// <param name="attestationEndpoint">Endpoint for attestation services</param>
        /// <param name="csr">Certificate Signing Request in PEM format</param>
        /// <param name="managedIdentityKeyInfo">Information about the key used for the CSR</param>
        /// <returns>Certificate request response containing the issued certificate</returns>
        /// <exception cref="MsalClientException">Thrown when attestation requirements aren't met</exception>
        /// <exception cref="MsalServiceException">Thrown for service communication errors</exception>
        private async Task<CertificateRequestResponse> ExecuteCertificateRequestAsync(
            string clientId,
            string attestationEndpoint,
            string csr,
            ManagedIdentityKeyInfo managedIdentityKeyInfo)
        {
            var queryParams = ImdsV2QueryParamsHelper(_requestContext);

            // TODO: add bypass_cache query param in case of token revocation. Boolean: true/false

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString() }
            };

            if (_isMtlsPopRequested && managedIdentityKeyInfo.Type != ManagedIdentityKeyType.KeyGuard)
            {
                throw new MsalClientException(
                    "mtls_pop_requires_keyguard",
                    "[ImdsV2] mTLS Proof-of-Possession requires a KeyGuard-backed key. Enable KeyGuard or use a KeyGuard-supported environment.");
            }

            // TODO: : Normalize and validate attestation endpoint Code needs to be removed 
            // once IMDS team start returning full URI
            Uri normalizedEndpoint = NormalizeAttestationEndpoint(attestationEndpoint, _requestContext.Logger);

            // Ask helper for JWT only for KeyGuard keys
            string attestationJwt = string.Empty;
            if (managedIdentityKeyInfo.Type == ManagedIdentityKeyType.KeyGuard)
            {
                attestationJwt = await GetAttestationJwtAsync(
                    clientId,
                    normalizedEndpoint,
                    managedIdentityKeyInfo,
                    _requestContext.UserCancellationToken).ConfigureAwait(false);
            }

            var certificateRequestBody = new CertificateRequestBody()
            {
                Csr = csr,
                AttestationToken = attestationJwt
            };

            string body = JsonHelper.SerializeToJson(certificateRequestBody);

            IRetryPolicyFactory retryPolicyFactory = _requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.Imds);

            HttpResponse response = null;

            try
            {
                response = await _requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                    ImdsManagedIdentitySource.GetValidatedEndpoint(_requestContext.Logger, CertificateRequestPath, queryParams),
                    headers,
                    body: new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                    method: HttpMethod.Post,
                    logger: _requestContext.Logger,
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken: _requestContext.UserCancellationToken,
                    retryPolicy: retryPolicy)
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                int? statusCode = response != null ? (int)response.StatusCode : (int?)null;

                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    $"[ImdsV2] ImdsV2ManagedIdentitySource.ExecuteCertificateRequestAsync failed.",
                    ex,
                    ManagedIdentitySource.ImdsV2,
                    statusCode);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    $"[ImdsV2] ImdsV2ManagedIdentitySource.ExecuteCertificateRequestAsync failed due to HTTP error. Status code: {response.StatusCode} Body: {response.Body}",
                    null,
                    ManagedIdentitySource.ImdsV2,
                    (int)response.StatusCode);
            }

            var certificateRequestResponse = JsonHelper.DeserializeFromJson<CertificateRequestResponse>(response.Body);
            CertificateRequestResponse.Validate(certificateRequestResponse);

            return certificateRequestResponse;
        }

        /// <summary>
        /// Creates an authentication request for the managed identity.
        /// This is the core method that implements the certificate-based authentication flow.
        /// </summary>
        /// <param name="resource">Target resource for which to acquire a token</param>
        /// <returns>A prepared managed identity request with appropriate certificate binding</returns>
        protected override async Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            // Lazy mint function: CSR + /issuecredential; manager attaches key & installs.
            var tokenType = _isMtlsPopRequested ? Constants.MtlsPoPTokenType : Constants.BearerTokenType;

            var csrMetadata = await GetCsrMetadataAsync(_requestContext, false).ConfigureAwait(false);
            var identityKey = _requestContext.ServiceBundle.Config.ClientId;
            var keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;

            var certMgr = new MsiCertManager(_requestContext);

            var (cert, resp) = await certMgr.GetOrMintBindingAsync(
                identityKey,
                tokenType,
                async ct =>
                {
                    var keyInfo = await keyProvider.GetOrCreateKeyAsync(_requestContext.Logger, ct).ConfigureAwait(false);
                    var (csr, privateKey) = _requestContext.ServiceBundle.Config.CsrFactory
                        .Generate(keyInfo.Key, csrMetadata.ClientId, csrMetadata.TenantId, csrMetadata.CuId);

                    var r = await ExecuteCertificateRequestAsync(
                        csrMetadata.ClientId, csrMetadata.AttestationEndpoint, csr, keyInfo).ConfigureAwait(false);

                    return (r, privateKey);
                },
                _requestContext.UserCancellationToken
            ).ConfigureAwait(false);

            var request = BuildTokenRequest(resource, resp.MtlsAuthenticationEndpoint, resp.TenantId, resp.ClientId, tokenType);
            request.MtlsCertificate = cert;
            request.CertificateRequestResponse = resp;
            return request;

        }

        /// <summary>
        /// Constructs a token request to the STS endpoint using the provided parameters.
        /// </summary>
        /// <param name="resource">Resource to acquire a token for</param>
        /// <param name="mtlsAuthenticationEndpoint">MTLS authentication endpoint from certificate response</param>
        /// <param name="tenantId">Tenant ID for the managed identity</param>
        /// <param name="clientId">Client ID for the managed identity</param>
        /// <param name="tokenType">Type of token to request (Bearer or PoP)</param>
        /// <returns>A prepared request object with appropriate headers and parameters</returns>
        private ManagedIdentityRequest BuildTokenRequest(string resource, string mtlsAuthenticationEndpoint, string tenantId, string clientId, string tokenType)
        {
            var stsUri = new Uri($"{mtlsAuthenticationEndpoint}/{tenantId}{AcquireEntraTokenPath}");

            var request = new ManagedIdentityRequest(HttpMethod.Post, stsUri)
            {
                RequestType = RequestType.STS
            };

            var idParams = MsalIdHelper.GetMsalIdParameters(_requestContext.Logger);
            foreach (var idParam in idParams)
            {
                request.Headers[idParam.Key] = idParam.Value;
            }

            request.Headers.Add(OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString());
            request.Headers.Add(ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue);
            request.Headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");

            request.BodyParameters.Add("client_id", clientId);
            request.BodyParameters.Add("grant_type", OAuth2GrantType.ClientCredentials);
            request.BodyParameters.Add("scope", resource.TrimEnd('/') + "/.default");
            request.BodyParameters.Add("token_type", tokenType);

            return request;
        }

        /// <summary>
        /// Creates query parameters for IMDSv2 API calls, including API version and user-assigned identity parameters when applicable.
        /// </summary>
        private static string ImdsV2QueryParamsHelper(RequestContext requestContext)
        {
            var queryParams = $"cred-api-version={ImdsV2ApiVersion}";

            var userAssignedIdQueryParam = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                requestContext.ServiceBundle.Config.ManagedIdentityId.IdType,
                requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId,
                requestContext.Logger);
            
            if (userAssignedIdQueryParam != null)
            {
                queryParams += $"&{userAssignedIdQueryParam.Value.Key}={userAssignedIdQueryParam.Value.Value}";
            }

            return queryParams;
        }

        /// <summary>
        /// Obtains an attestation JWT for the KeyGuard/CSR payload using the configured
        /// attestation provider and normalized endpoint.
        /// </summary>
        /// <param name="clientId">Client ID to be sent to the attestation provider.</param>
        /// <param name="attestationEndpoint">The attestation endpoint.</param>
        /// <param name="keyInfo">The key information.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JWT string suitable for the IMDSv2 attested POP flow.</returns>
        /// <exception cref="MsalClientException">Wraps client/network failures.</exception>

        private async Task<string> GetAttestationJwtAsync(
            string clientId, 
            Uri attestationEndpoint, 
            ManagedIdentityKeyInfo keyInfo, 
            CancellationToken cancellationToken)
        {
            // Provider is a local dependency; missing provider is a client error
            var provider = _requestContext.AttestationTokenProvider;

            // KeyGuard requires RSACng on Windows
            if (keyInfo.Type == ManagedIdentityKeyType.KeyGuard &&
                keyInfo.Key is not System.Security.Cryptography.RSACng rsaCng)
            {
                throw new MsalClientException(
                    "keyguard_requires_cng",
                    "[ImdsV2] KeyGuard attestation currently supports only RSA CNG keys on Windows.");
            }

            // Attestation token input
            var input = new AttestationTokenInput
            {
                ClientId = clientId,
                AttestationEndpoint = attestationEndpoint,
                KeyHandle = (keyInfo.Key as System.Security.Cryptography.RSACng)?.Key.Handle
            };

            // response from provider 
            var response = await provider(input, cancellationToken).ConfigureAwait(false);

            // Validate response
            if (response == null || string.IsNullOrWhiteSpace(response.AttestationToken))
            {
                throw new MsalClientException(
                    "attestation_failed",
                    "[ImdsV2] Attestation provider failed to return an attestation token.");
            }

            // Return the JWT
            return response.AttestationToken;
        }

        //To-do : Remove this method once IMDS team start returning full URI
        /// <summary>
        /// Temporarily normalize attestation endpoint values to a full https:// URI.
        /// IMDS team will eventually return a full URI. 
        /// </summary>
        /// <param name="rawEndpoint"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static Uri NormalizeAttestationEndpoint(string rawEndpoint, ILoggerAdapter logger)
        {
            if (string.IsNullOrWhiteSpace(rawEndpoint))
            {
                return null;
            }

            // Trim whitespace
            rawEndpoint = rawEndpoint.Trim();

            // If it already parses as an absolute URI with https, keep it.
            if (Uri.TryCreate(rawEndpoint, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
            {
                return absolute;
            }

            // If it has no scheme (common service behavior returning only host)
            // prepend https:// and try again.
            if (!rawEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = "https://" + rawEndpoint;
                if (Uri.TryCreate(candidate, UriKind.Absolute, out var httpsUri))
                {
                    logger.Info(() => $"[Managed Identity] Normalized attestation endpoint '{rawEndpoint}' -> '{httpsUri.ToString()}'.");
                    return httpsUri;
                }
            }

            // Final attempt: reject http (non‑TLS) or malformed
            if (Uri.TryCreate(rawEndpoint, UriKind.Absolute, out var anyUri))
            {
                if (!anyUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warning($"[Managed Identity] Attestation endpoint uses unsupported scheme '{anyUri.Scheme}'. HTTPS is required.");
                    return null;
                }
                return anyUri;
            }

            logger.Warning($"[Managed Identity] Failed to normalize attestation endpoint value '{rawEndpoint}'.");
            return null;
        }

        /// <summary>
        /// Caches binding metadata for a specific identity and token type.
        /// This allows certificate reuse across authentication requests for the same identity.
        /// </summary>
        /// <param name="identityKey">The identity key (client ID)</param>
        /// <param name="resp">Certificate response data</param>
        /// <param name="subject">Certificate subject DN</param>
        /// <param name="thumbprint">Certificate thumbprint</param>
        /// <param name="tokenType">Token type (Bearer or PoP)</param>
        /// <remarks>
        /// The subject is set only once per identity (first-wins) while thumbprints may update
        /// during certificate rotation.
        /// </remarks>
        internal static void CacheImdsV2BindingMetadata(
            string identityKey,
            CertificateRequestResponse resp,
            string subject,
            string thumbprint,
            string tokenType)
        {
            if (string.IsNullOrEmpty(identityKey) || resp == null ||
                string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(thumbprint))
            {
                return;
            }

            var meta = ManagedIdentityClient.s_identityToBindingMetadataMap
                .GetOrAdd(identityKey, _ => new ImdsV2BindingMetadata());

            meta.Subject ??= subject;

            if (string.Equals(tokenType, Constants.MtlsPoPTokenType, StringComparison.OrdinalIgnoreCase))
            {
                meta.PopResponse = resp;
                meta.PopThumbprint = thumbprint;
            }
            else
            {
                meta.BearerResponse = resp;
                meta.BearerThumbprint = thumbprint;
            }
        }

        /// <summary>
        /// Attempts to retrieve binding metadata for a specific identity and token type.
        /// </summary>
        /// <param name="identityKey">The identity key (client ID) to look up</param>
        /// <param name="tokenType">Token type (Bearer or PoP)</param>
        /// <param name="resp">Output parameter for the certificate response</param>
        /// <param name="subject">Output parameter for the certificate subject</param>
        /// <param name="thumbprint">Output parameter for the certificate thumbprint</param>
        /// <returns>True if binding metadata was found, false otherwise</returns>
        internal static bool TryGetImdsV2BindingMetadata(
            string identityKey,
            string tokenType,
            out CertificateRequestResponse resp,
            out string subject,
            out string thumbprint)
        {
            resp = null;
            subject = null;
            thumbprint = null;

            if (string.IsNullOrEmpty(identityKey))
                return false;

            if (!ManagedIdentityClient.s_identityToBindingMetadataMap.TryGetValue(identityKey, out var meta) ||
                meta == null || string.IsNullOrEmpty(meta.Subject))
            {
                return false;
            }

            subject = meta.Subject;

            if (string.Equals(tokenType, Constants.MtlsPoPTokenType, StringComparison.OrdinalIgnoreCase))
            {
                resp = meta.PopResponse;
                thumbprint = meta.PopThumbprint;
            }
            else
            {
                resp = meta.BearerResponse;
                thumbprint = meta.BearerThumbprint;
            }

            return resp != null && !string.IsNullOrEmpty(thumbprint);
        }

        /// <summary>
        /// Attempts to retrieve any available PoP binding metadata from any identity.
        /// This is primarily used for test scenarios or when sharing certificates across identities.
        /// Only applies to PoP tokens - Bearer tokens must match the specific identity.
        /// </summary>
        /// <param name="tokenType">Token type (must be PoP)</param>
        /// <param name="resp">Output parameter for the certificate response</param>
        /// <param name="subject">Output parameter for the certificate subject</param>
        /// <param name="thumbprint">Output parameter for the certificate thumbprint</param>
        /// <returns>True if any binding metadata was found, false otherwise</returns>
        internal static bool TryGetAnyImdsV2BindingMetadata(
            string tokenType,
            out CertificateRequestResponse resp,
            out string subject,
            out string thumbprint)
        {
            resp = null;
            subject = null;
            thumbprint = null;

            if (!string.Equals(tokenType, Constants.MtlsPoPTokenType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var kv in ManagedIdentityClient.s_identityToBindingMetadataMap)
            {
                var m = kv.Value;
                if (m?.PopResponse != null && !string.IsNullOrEmpty(m.PopThumbprint) && !string.IsNullOrEmpty(m.Subject))
                {
                    resp = m.PopResponse;
                    subject = m.Subject;
                    thumbprint = m.PopThumbprint;
                    return true;
                }
            }

            return false;
        }
    }
}
