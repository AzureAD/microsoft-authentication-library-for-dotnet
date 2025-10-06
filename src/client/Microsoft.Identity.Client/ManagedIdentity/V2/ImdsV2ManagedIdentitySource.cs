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
    internal partial class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
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

        protected override async Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            var csrMetadata = await GetCsrMetadataAsync(_requestContext, false).ConfigureAwait(false);
            string identityKey = _requestContext.ServiceBundle.Config.ClientId;

            IManagedIdentityKeyProvider keyProvider = _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;

            // Reuse path: read IMDSv2 metadata + cert subject and reload cert from user store
            // Prefer per‑identity reuse first (subject + metadata)
            // per-identity reuse branch
            if (!string.IsNullOrEmpty(identityKey) &&
                TryGetImdsV2BindingMetadata(identityKey, out var resp, out var subject))
            {
                var cert = MtlsCertStore.FindFreshestBySubject(subject, cleanupOlder: true);
                if (MtlsCertStore.IsCurrentlyValid(cert))
                {
                    var tokenType = _isMtlsPopRequested ? Constants.MtlsPoPTokenType : Constants.BearerTokenType;

                    var request = BuildTokenRequest(
                        resource,
                        resp.MtlsAuthenticationEndpoint, // endpoint from metadata
                        csrMetadata.TenantId,            // CURRENT identity
                        csrMetadata.ClientId,            // CURRENT identity
                        tokenType);

                    request.MtlsCertificate = cert;
                    request.CertificateRequestResponse = resp;

                    if (MtlsCertStore.IsBeyondHalfLife(cert))
                    {
                        _requestContext.Logger.Info("[IMDSv2] mTLS binding at/after half-life (reused for this request).");
                    }

                    return request;
                }

                _requestContext.Logger.Info("[IMDSv2] No usable mTLS binding found; minting a new one.");
            }

            // PoP-only cross-identity binding reuse, but with the CURRENT identity’s identity parameters
            // Cross-identity PoP fallback: reuse an existing user-store binding,
            // but ALWAYS use the CURRENT identity’s client/tenant from csrMetadata.
            if (_isMtlsPopRequested &&
                TryGetAnyImdsV2BindingMetadata(out var anyResp, out var anySubject))
            {
                var cert = MtlsCertStore.FindFreshestBySubject(anySubject, cleanupOlder: true);
                if (MtlsCertStore.IsCurrentlyValid(cert))
                {
                    if (!string.IsNullOrEmpty(identityKey))
                    {
                        CacheImdsV2BindingMetadata(identityKey, anyResp, anySubject);
                    }

                    var request = BuildTokenRequest(
                        resource,
                        anyResp.MtlsAuthenticationEndpoint,  // reuse endpoint
                        csrMetadata.TenantId,                // use CURRENT identity's tenant
                        csrMetadata.ClientId,                // use CURRENT identity's client
                        Constants.MtlsPoPTokenType);

                    request.MtlsCertificate = cert;
                    request.CertificateRequestResponse = anyResp;

                    // optional: log half-life, rotation hook later
                    if (MtlsCertStore.IsBeyondHalfLife(cert))
                    {
                        _requestContext.Logger.Info("[IMDSv2] mTLS binding at/after half-life (reused for this request).");
                    }

                    return request;
                }
            }

            // Mint binding certificate
            ManagedIdentityKeyInfo keyInfo = await keyProvider
                .GetOrCreateKeyAsync(_requestContext.Logger, _requestContext.UserCancellationToken)
                .ConfigureAwait(false);

            var (csr, privateKey) = _requestContext.ServiceBundle.Config.CsrFactory
                .Generate(keyInfo.Key, csrMetadata.ClientId, csrMetadata.TenantId, csrMetadata.CuId);

            var certificateRequestResponse = await ExecuteCertificateRequestAsync(
                csrMetadata.ClientId, csrMetadata.AttestationEndpoint, csr, keyInfo).ConfigureAwait(false);

            // Attach private key
            var mtlsCertificate = CommonCryptographyManager.AttachPrivateKeyToCert(
                certificateRequestResponse.Certificate, privateKey);

            // Install + remember subject (prune older)
            subject = MtlsBindingStore.InstallAndGetSubject(mtlsCertificate, _requestContext.Logger);
            MtlsBindingStore.PruneOlder(subject, mtlsCertificate.Thumbprint, _requestContext.Logger);

            if (!string.IsNullOrEmpty(identityKey))
            {
                CacheImdsV2BindingMetadata(identityKey, certificateRequestResponse, subject);
                _requestContext.Logger.Info("[IMDSv2] Minted mTLS binding and cached IMDSv2 metadata + subject.");
            }
            else
            {
                _requestContext.Logger.Warning("[IMDSv2] Missing identity key; skipping metadata cache.");
            }

            string tokenTypeFinal = _isMtlsPopRequested ? Constants.MtlsPoPTokenType : Constants.BearerTokenType;
            var finalRequest = BuildTokenRequest(resource, certificateRequestResponse.MtlsAuthenticationEndpoint, certificateRequestResponse.TenantId, certificateRequestResponse.ClientId, tokenTypeFinal);
            finalRequest.MtlsCertificate = mtlsCertificate;
            finalRequest.CertificateRequestResponse = certificateRequestResponse;
            return finalRequest;
        }

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

        internal static void CacheImdsV2BindingMetadata(string identityKey, CertificateRequestResponse resp, string certSubject)
        {
            if (string.IsNullOrEmpty(identityKey) || resp == null)
                return;

            ManagedIdentityClient.s_identityToBindingMetadataMap[identityKey] =
                new ImdsV2BindingMetadata { Response = resp, CertificateSubject = certSubject };
        }

        internal static bool TryGetImdsV2BindingMetadata(string identityKey, out CertificateRequestResponse resp, out string certSubject)
        {
            resp = null;
            certSubject = null;
            if (string.IsNullOrEmpty(identityKey))
                return false;

            if (ManagedIdentityClient.s_identityToBindingMetadataMap
                .TryGetValue(identityKey, out var meta) && meta?.Response != null)
            {
                resp = meta.Response;
                certSubject = meta.CertificateSubject;
                return true;
            }
            return false;
        }

        internal static bool TryGetAnyImdsV2BindingMetadata(out CertificateRequestResponse resp, out string certSubject)
        {
            resp = null;
            certSubject = null;

            foreach (var kvp in ManagedIdentityClient.s_identityToBindingMetadataMap)
            {
                var meta = kvp.Value;
                if (meta?.Response != null && !string.IsNullOrWhiteSpace(meta.CertificateSubject))
                {
                    resp = meta.Response;
                    certSubject = meta.CertificateSubject;
                    return true;
                }
            }

            return false;
        }
    }
}
