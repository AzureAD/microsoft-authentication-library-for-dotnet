// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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

        private readonly IMtlsCertificateCache _mtlsCache;
        private Func<string, SafeHandle, string, CancellationToken, Task<string>> _attestationTokenProvider;

        // used in unit tests
        public const string ApiVersionQueryParam = "cred-api-version";
        public const string ImdsV2ApiVersion = "2.0";
        public const string CsrMetadataPath = "/metadata/identity/getplatformmetadata";
        public const string CertificateRequestPath = "/metadata/identity/issuecredential";
        public const string AcquireEntraTokenPath = "/oauth2/v2.0/token";

        public static async Task<CsrMetadata> GetCsrMetadataAsync(RequestContext requestContext)
        {
            var queryParams = ImdsManagedIdentitySource.ImdsQueryParamsHelper(requestContext, ApiVersionQueryParam, ImdsV2ApiVersion);

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { OAuth2Header.XMsCorrelationId, requestContext.CorrelationId.ToString() }
            };

            IRetryPolicyFactory retryPolicyFactory = requestContext.ServiceBundle.Config.RetryPolicyFactory;
            IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.Imds);

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
                ThrowCsrMetadataRequestException(
                    "ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed.",
                    ex);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                ThrowCsrMetadataRequestException(
                    $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed due to HTTP error. Status code: {response.StatusCode} Body: {response.Body}",
                    null,
                    (int)response.StatusCode);
            }

            if (!ValidateCsrMetadataResponse(response, requestContext.Logger))
            {
                return null;
            }

            return TryCreateCsrMetadata(response, requestContext.Logger);
        }

        private static void ThrowCsrMetadataRequestException(
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
            ILoggerAdapter logger)
        {
            string serverHeader = response.HeadersAsDictionary
                .FirstOrDefault((kvp) => {
                    return string.Equals(kvp.Key, "server", StringComparison.OrdinalIgnoreCase);
                }).Value;

            if (serverHeader == null)
            {
                ThrowCsrMetadataRequestException(
                    $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because response doesn't have server header. Status code: {response.StatusCode} Body: {response.Body}",
                    null,
                    (int)response.StatusCode);
            }

            if (!serverHeader.Contains("IMDS", StringComparison.OrdinalIgnoreCase))
            {
                ThrowCsrMetadataRequestException(
                    $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because the 'server' header format is invalid. Extracted server header: {serverHeader}. Status code: {response.StatusCode} Body: {response.Body}",
                    null,
                    (int)response.StatusCode);
            }

            return true;
        }

        private static CsrMetadata TryCreateCsrMetadata(
            HttpResponse response,
            ILoggerAdapter logger)
        {
            CsrMetadata csrMetadata = JsonHelper.DeserializeFromJson<CsrMetadata>(response.Body);
            if (!CsrMetadata.ValidateCsrMetadata(csrMetadata))
            {
                ThrowCsrMetadataRequestException(
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

        internal ImdsV2ManagedIdentitySource(RequestContext requestContext) 
            : this(requestContext,  
                  new MtlsBindingCache(s_mtlsCertificateCache, PersistentCertificateCacheFactory
                      .Create(requestContext.Logger)))
        {
        }

        internal ImdsV2ManagedIdentitySource(
            RequestContext requestContext,
            IMtlsCertificateCache mtlsCache)
            : base(requestContext, ManagedIdentitySource.ImdsV2)
        {
            _mtlsCache = mtlsCache ?? throw new ArgumentNullException(nameof(mtlsCache));
        }

        public override async Task<ManagedIdentityResponse> AuthenticateAsync(
            ApiConfig.Parameters.AcquireTokenForManagedIdentityParameters parameters,
            CancellationToken cancellationToken)
        {
            // Capture the attestation token provider delegate before calling base
            _attestationTokenProvider = parameters.AttestationTokenProvider;
            return await base.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CertificateRequestResponse> ExecuteCertificateRequestAsync(
            string clientId,
            string attestationEndpoint,
            string csr,
            ManagedIdentityKeyInfo managedIdentityKeyInfo)
        {
            var queryParams = ImdsManagedIdentitySource.ImdsQueryParamsHelper(_requestContext, ApiVersionQueryParam, ImdsV2ApiVersion);

            // TODO: add bypass_cache query param in case of token revocation. Boolean: true/false

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString() }
            };

            // Attempt attestation only for KeyGuard keys when provider is available
            // For non-KeyGuard keys (Hardware, InMemory), proceed with non-attested flow
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
            else
            {
                _requestContext.Logger.Info($"[ImdsV2] Using {managedIdentityKeyInfo.Type} key. Proceeding with non-attested mTLS PoP flow.");
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
            CsrMetadata csrMetadata = await GetCsrMetadataAsync(_requestContext).ConfigureAwait(false);

            // Early validation: Fail-fast if mTLS PoP was requested but KeyGuard is unavailable.
            // This check happens before any network calls to avoid wasted round-trips.
            // Note: This creates/retrieves the key, but on cache hit scenarios (below),
            // this may be the only key access needed.
            if (_isMtlsPopRequested)
            {
                IManagedIdentityKeyProvider keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;
                ManagedIdentityKeyInfo keyInfo = await keyProvider
                    .GetOrCreateKeyAsync(_requestContext.Logger, _requestContext.UserCancellationToken)
                    .ConfigureAwait(false);

                if (keyInfo.Type != ManagedIdentityKeyType.KeyGuard)
                {
                    throw new MsalClientException(
                        "mtls_pop_requires_keyguard",
                        $"[ImdsV2] mTLS Proof-of-Possession requires KeyGuard keys. Current key type: {keyInfo.Type}");
                }
            }

            string certCacheKey = _requestContext.ServiceBundle.Config.ClientId;

            // Get or create mTLS binding (cert + endpoint + client_id) from cache.
            // The factory delegate only executes on cache miss.
            MtlsBindingInfo mtlsBinding = await GetOrCreateMtlsBindingAsync(
                cacheKey: certCacheKey,
                async () =>  // Factory: only invoked if cert is not in cache
                {
                    IManagedIdentityKeyProvider keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;

                    // Second GetOrCreateKeyAsync call: Required for CSR generation on cache miss.
                    // If the cert is cached, this entire factory delegate is skipped.
                    // If validation above succeeded, this call returns the same cached key immediately.
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

                    return new MtlsBindingInfo(mtlsCertificate, endpointBase, clientIdGuid);

                },
                _requestContext.UserCancellationToken,
                _requestContext.Logger)
                .ConfigureAwait(false);

            X509Certificate2 bindingCertificate = mtlsBinding.Certificate;
            string endpointBaseForToken = mtlsBinding.Endpoint;
            string clientIdForToken = mtlsBinding.ClientId;

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

        /// <summary>
        /// Obtains an attestation JWT for the Credential Guard/CSR payload using the configured
        /// attestation token provider delegate.
        /// </summary>
        /// <param name="clientId">Client ID to be sent to the attestation provider.</param>
        /// <param name="attestationEndpoint">The attestation endpoint.</param>
        /// <param name="keyInfo">The key information.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JWT string suitable for the IMDSv2 attested PoP flow, or null for non-attested flow.</returns>
        private async Task<string> GetAttestationJwtAsync(
            string clientId, 
            Uri attestationEndpoint, 
            ManagedIdentityKeyInfo keyInfo, 
            CancellationToken cancellationToken)
        {
            // Check if attestation token provider has been configured
            if (_attestationTokenProvider == null)
            {
                _requestContext.Logger.Info("[ImdsV2] Attestation token provider not configured. Proceeding with non-attested flow.");
                return null; // Null attestation token indicates non-attested flow
            }

            // Credential Guard requires RSACng on Windows
            if (keyInfo.Key is not System.Security.Cryptography.RSACng rsaCng)
            {
                throw new MsalClientException(
                    "credential_guard_requires_cng",
                    "[ImdsV2] Credential Guard attestation currently supports only RSA CNG keys on Windows.");
            }

            try
            {
                // Call the attestation token provider delegate
                string attestationJwt = await _attestationTokenProvider(
                    attestationEndpoint.AbsoluteUri,
                    rsaCng.Key.Handle,
                    clientId,
                    cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(attestationJwt))
                {
                    _requestContext.Logger.Info("[ImdsV2] Attestation provider returned null/empty JWT. Proceeding with non-attested flow.");
                    return null;
                }

                return attestationJwt;
            }
            catch (Exception ex)
            {
                throw new MsalClientException(
                    "attestation_failed",
                    $"[ImdsV2] Attestation token provider failed: {ex.Message}",
                    ex);
            }
        }

        private Task<MtlsBindingInfo> GetOrCreateMtlsBindingAsync(
            string cacheKey,
            Func<Task<MtlsBindingInfo>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            return _mtlsCache.GetOrCreateAsync(cacheKey, factory, cancellationToken, logger);
        }

        internal static void ResetCertCacheForTest()
        {
            // Clear caches so each test starts fresh
            if (s_mtlsCertificateCache != null)
            {
                s_mtlsCertificateCache.Clear();
            }
        }
    }
}
