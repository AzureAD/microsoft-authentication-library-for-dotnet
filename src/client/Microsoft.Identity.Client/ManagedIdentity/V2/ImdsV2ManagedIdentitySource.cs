// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
    {
        // used in unit tests
        public const string ImdsV2ApiVersion = "2.0";
        public const string CsrMetadataPath = "/metadata/identity/getplatformmetadata";
        public const string CertificateRequestPath = "/metadata/identity/issuecredential";
        public const string AcquireEntraTokenPath = "/oauth2/v2.0/token";

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

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            return new ImdsV2ManagedIdentitySource(requestContext);
        }

        internal ImdsV2ManagedIdentitySource(RequestContext requestContext) :
            base(requestContext, ManagedIdentitySource.ImdsV2)
        { }

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

            if (managedIdentityKeyInfo.Type != ManagedIdentityKeyType.KeyGuard)
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
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    $"[ImdsV2] ImdsV2ManagedIdentitySource.ExecuteCertificateRequestAsync failed.",
                    ex,
                    ManagedIdentitySource.ImdsV2,
                    (int)response.StatusCode);
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

        protected override async Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            var csrMetadata = await GetCsrMetadataAsync(_requestContext, false).ConfigureAwait(false);

            IManagedIdentityKeyProvider keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;

            ManagedIdentityKeyInfo keyInfo = await keyProvider
                .GetOrCreateKeyAsync(
                _requestContext.Logger,
                _requestContext.UserCancellationToken)
                .ConfigureAwait(false);

            var (csr, privateKey) = _requestContext.ServiceBundle.Config.CsrFactory.Generate(keyInfo.Key, csrMetadata.ClientId, csrMetadata.TenantId, csrMetadata.CuId);

            var certificateRequestResponse = await ExecuteCertificateRequestAsync(
                csrMetadata.ClientId,
                csrMetadata.AttestationEndpoint,
                csr,
                keyInfo).ConfigureAwait(false);

            // transform certificateRequestResponse.Certificate to x509 with private key
            var mtlsCertificate = CommonCryptographyManager.AttachPrivateKeyToCert(
                certificateRequestResponse.Certificate,
                privateKey);

            ManagedIdentityRequest request = new(HttpMethod.Post, new Uri($"{certificateRequestResponse.MtlsAuthenticationEndpoint}/{certificateRequestResponse.TenantId}{AcquireEntraTokenPath}"));

            var idParams = MsalIdHelper.GetMsalIdParameters(_requestContext.Logger);
            foreach (var idParam in idParams)
            {
                request.Headers[idParam.Key] = idParam.Value;
            }
            request.Headers.Add(OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString());
            request.Headers.Add(ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue);
            request.Headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");

            request.BodyParameters.Add("client_id", certificateRequestResponse.ClientId);
            request.BodyParameters.Add("grant_type", OAuth2GrantType.ClientCredentials);
            request.BodyParameters.Add("scope", resource.TrimEnd('/') + "/.default");
            request.BodyParameters.Add("token_type", "mtls_pop");

            request.RequestType = RequestType.STS;

            request.MtlsCertificate = mtlsCertificate;

            return request;
        }

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
        /// attestation provider and HybridCache for performance optimization.
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
            if (keyInfo.Type == ManagedIdentityKeyType.KeyGuard &&
                keyInfo.Key is not System.Security.Cryptography.RSACng rsaCng)
            {
                throw new MsalClientException(
                    "keyguard_requires_cng",
                    "[ImdsV2] KeyGuard attestation currently supports only RSA CNG keys on Windows.");
            }

            // Extract cache key from KeyHandle
            long cacheKey = GetCacheKeyFromKeyInfo(keyInfo);

            _requestContext.Logger.Verbose(() => $"[ImdsV2] GetAttestationJwtAsync called for cache key: {cacheKey}");

            var cache = new HybridCache(_requestContext.Logger);

            // Step 1: Check cache first
            var cached = await cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                _requestContext.Logger.Info(() => $"[ImdsV2] Attestation token cache hit for key: {cacheKey}");
                return cached.AttestationToken;
            }

            _requestContext.Logger.Info(() => $"[ImdsV2] Attestation token cache miss for key: {cacheKey}, minting new token");

            // Step 2: Cache miss - mint new token via provider
            var provider = _requestContext.AttestationTokenProvider;
            var input = new AttestationTokenInput
            {
                ClientId = clientId,
                AttestationEndpoint = attestationEndpoint,
                KeyHandle = (keyInfo.Key as System.Security.Cryptography.RSACng)?.Key.Handle
            };

            var minted = await provider(input, cancellationToken).ConfigureAwait(false);
            if (minted == null || string.IsNullOrWhiteSpace(minted.AttestationToken))
            {
                throw new MsalClientException(
                    "attestation_failed",
                    "[ImdsV2] Attestation provider failed to return an attestation token.");
            }

            // Step 3: Cache the new token for 8 hours (MAA default TTL)
            var expiresOn = DateTimeOffset.UtcNow + TimeSpan.FromHours(8);
            try
            {
                await cache.SetAsync(cacheKey, minted.AttestationToken, expiresOn, cancellationToken).ConfigureAwait(false);
                _requestContext.Logger.Info(() => $"[ImdsV2] Attestation token successfully cached for key: {cacheKey}");
            }
            catch (Exception ex)
            {
                _requestContext.Logger.Warning($"[ImdsV2] Error caching attestation token for key {cacheKey}: {ex.Message}");
                // Cache failure is not critical - return the token anyway
            }

            return minted.AttestationToken;
        }

        /// <summary>
        /// Extracts a cache key from the managed identity key information.
        /// </summary>
        /// <param name="keyInfo">The managed identity key information containing the KeyHandle.</param>
        /// <returns>
        /// A long integer representing the KeyHandle pointer value, or 0 if extraction fails.
        /// The returned value is used as a unique identifier for cache entries across processes.
        /// </returns>
        /// <remarks>
        /// This method:
        /// - Safely extracts the pointer value from the KeyHandle using DangerousGetHandle()
        /// - Converts the pointer to a 64-bit integer for use as a cache key
        /// - Returns 0 as a fallback key if extraction fails for any reason
        /// - Uses defensive programming to handle invalid handles gracefully
        /// 
        /// The use of DangerousGetHandle() is acceptable here because:
        /// - The handle lifetime is managed by the caller
        /// - We only extract the pointer value, not use it for memory access
        /// - The extracted value is used immediately for cache key generation
        /// 
        /// Cross-Process Behavior:
        /// - Different KeyHandle instances with the same underlying pointer value
        ///   will produce the same cache key, enabling cross-process cache sharing
        /// - Cache key 0 is used as a fallback when handle extraction fails
        /// - The same key will be generated across different application instances
        /// </remarks>
        private static long GetCacheKeyFromKeyInfo(ManagedIdentityKeyInfo keyInfo)
        {
            try
            {
                if (keyInfo.Key is System.Security.Cryptography.RSACng rsaCng &&
                    rsaCng.Key.Handle != null &&
                    !rsaCng.Key.Handle.IsInvalid)
                {
                    return rsaCng.Key.Handle.DangerousGetHandle().ToInt64();
                }
            }
            catch { /* ignore extraction errors and use fallback key */ }
            return 0L;
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
    }
}
