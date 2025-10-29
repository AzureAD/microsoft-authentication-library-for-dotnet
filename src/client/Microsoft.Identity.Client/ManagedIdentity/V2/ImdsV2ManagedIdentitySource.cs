// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
    {
        // Central, process-local cache for mTLS binding (cert + endpoint + canonical client_id).
        internal static readonly ICertificateCache s_mtlsCertificateCache = new InMemoryCertificateCache();

        // Per-key async de-duplication so concurrent callers don’t double-mint.
        internal static readonly ConcurrentDictionary<string, SemaphoreSlim> s_perKeyGates =
            new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);

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

            // Ask helper for JWT only for KeyGuard keys
            string attestationJwt = string.Empty;
            var attestationUri = new Uri(attestationEndpoint);

            if (managedIdentityKeyInfo.Type == ManagedIdentityKeyType.KeyGuard)
            {
                attestationJwt = await GetAttestationJwtAsync(
                    clientId,
                    attestationUri,
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
                int? statusCode = response != null ? (int?)response.StatusCode : null;

                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    "[ImdsV2] ImdsV2ManagedIdentitySource.ExecuteCertificateRequestAsync failed.",
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

        protected override async Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            var csrMetadata = await GetCsrMetadataAsync(_requestContext, false).ConfigureAwait(false);

            string certCacheKey = _requestContext.ServiceBundle.Config.ClientId;

            var certEndpointAndClientId = await GetOrCreateMtlsBindingAsync(
                cacheKey: certCacheKey,
                async () =>
                {
                    IManagedIdentityKeyProvider keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;

                    ManagedIdentityKeyInfo keyInfo = await keyProvider
                        .GetOrCreateKeyAsync(_requestContext.Logger, _requestContext.UserCancellationToken)
                        .ConfigureAwait(false);

                    var csrAndKey = _requestContext.ServiceBundle.Config.CsrFactory.Generate(
                        keyInfo.Key,
                        csrMetadata.ClientId,
                        csrMetadata.TenantId,
                        csrMetadata.CuId);

                    string csr = csrAndKey.csrPem;
                    var privateKey = csrAndKey.privateKey;

                    var certificateRequestResponse = await ExecuteCertificateRequestAsync(
                        csrMetadata.ClientId,
                        csrMetadata.AttestationEndpoint,
                        csr,
                        keyInfo).ConfigureAwait(false);

                    X509Certificate2 mtlsCertificate = CommonCryptographyManager.AttachPrivateKeyToCert(
                        certificateRequestResponse.Certificate,
                        privateKey);

                    // Base endpoint = "{mtlsAuthEndpoint}/{tenantId}"
                    string endpointBase =
                        (certificateRequestResponse.MtlsAuthenticationEndpoint).TrimEnd('/') +
                        "/" +
                        (certificateRequestResponse.TenantId).Trim('/');

                    // Canonical GUID to use as client_id in the token call
                    string clientIdGuid = certificateRequestResponse.ClientId;

                    return Tuple.Create(mtlsCertificate, endpointBase, clientIdGuid);
                },
                _requestContext.UserCancellationToken, 
                _requestContext.Logger)
                .ConfigureAwait(false);

            X509Certificate2 bindingCertificate = certEndpointAndClientId.Item1;
            string endpointBaseForToken = certEndpointAndClientId.Item2;
            string clientIdForToken = certEndpointAndClientId.Item3;

            ManagedIdentityRequest request = new ManagedIdentityRequest(
                HttpMethod.Post,
                new Uri(endpointBaseForToken + AcquireEntraTokenPath));

            Dictionary<string, string> idParams = MsalIdHelper.GetMsalIdParameters(_requestContext.Logger);

            foreach (KeyValuePair<string, string> idParam in idParams)
            {
                request.Headers[idParam.Key] = idParam.Value;
            }

            request.Headers.Add(OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString());
            request.Headers.Add(ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue);
            request.Headers.Add(OAuth2Header.RequestCorrelationIdInResponse, "true");

            var tokenType = _isMtlsPopRequested ? Constants.MtlsPoPTokenType : Constants.BearerTokenType;

            request.BodyParameters.Add("client_id", clientIdForToken);
            request.BodyParameters.Add("grant_type", OAuth2GrantType.ClientCredentials);
            request.BodyParameters.Add("scope", resource.TrimEnd('/') + "/.default");
            request.BodyParameters.Add("token_type", tokenType);

            request.RequestType = RequestType.STS;
            request.MtlsCertificate = bindingCertificate;

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

        // ...unchanged usings and class header...

        /// <summary>
        /// Read-through cache: try cache; if missing, run async factory once (per key),
        /// store the result, and return it. Thread-safe for the given cacheKey.
        /// </summary>
        private static async Task<Tuple<X509Certificate2, string, string>> GetOrCreateMtlsBindingAsync(
            string cacheKey,
            Func<Task<Tuple<X509Certificate2, string, string>>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("cacheKey must be non-empty.", nameof(cacheKey));
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            X509Certificate2 cachedCertificate;
            string cachedEndpointBase;
            string cachedClientId;

            // 1) Only lookup by cacheKey
            if (s_mtlsCertificateCache.TryGet(cacheKey, out var cached, logger))
            {
                cachedCertificate = cached.Certificate;
                cachedEndpointBase = cached.Endpoint;
                cachedClientId = cached.ClientId;

                return Tuple.Create(cachedCertificate, cachedEndpointBase, cachedClientId);
            }

            // 2) Gate per cacheKey
            var gate = s_perKeyGates.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Re-check after acquiring the gate
                if (s_mtlsCertificateCache.TryGet(cacheKey, out cached, logger))
                {
                    cachedCertificate = cached.Certificate;
                    cachedEndpointBase = cached.Endpoint;
                    cachedClientId = cached.ClientId;
                    return Tuple.Create(cachedCertificate, cachedEndpointBase, cachedClientId);
                }

                // 3) Mint + cache under the provided cacheKey
                var created = await factory().ConfigureAwait(false);

                s_mtlsCertificateCache.Set(
                    cacheKey, created.Item1, created.Item2, created.Item3, logger);

                return created;
            }
            finally
            {
                gate.Release();
            }
        }

        internal static void ResetCertCacheForTest()
        {
            // Clear caches so each test starts fresh
            if (s_mtlsCertificateCache != null)
            {
                s_mtlsCertificateCache.Clear();
            }

            foreach (var gate in s_perKeyGates.Values)
            {
                try
                { gate.Dispose(); }
                catch { }
            }
            s_perKeyGates.Clear();
        }
    }
}
