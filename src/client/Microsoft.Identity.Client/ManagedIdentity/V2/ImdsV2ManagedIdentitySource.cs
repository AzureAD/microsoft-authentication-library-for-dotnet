// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            requestContext.Logger.Info(() => "[Managed Identity] IMDSv2 flow is not supported on .NET Framework 4.6.2. Cryptographic operations required for managed identity authentication are unavailable on this platform. Skipping IMDSv2 probe.");
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
                    requestContext.Logger.Info(() => $"[Managed Identity] IMDSv2 CSR endpoint failure. Exception occurred while sending request to CSR metadata endpoint: ${ex}");
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
            string csr,
            string attestationEndpoint,
            ManagedIdentityKeyInfo managedIdentityKeyInfo)
        {
            var queryParams = ImdsV2QueryParamsHelper(_requestContext);

            // TODO: add bypass_cache query param in case of token revocation. Boolean: true/false

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { OAuth2Header.XMsCorrelationId, _requestContext.CorrelationId.ToString() }
            };

            string attestationJwt = string.Empty;

            // Normalize endpoint
            string normalizedEndpoint = NormalizeAttestationEndpoint(attestationEndpoint, _requestContext.Logger);
            if (string.IsNullOrEmpty(normalizedEndpoint))
            {
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    "attestation_endpoint_invalid",
                    $"[ImdsV2] Attestation endpoint '{attestationEndpoint}' is invalid or unsupported.",
                    null, ManagedIdentitySource.ImdsV2, null);
            }

            if (managedIdentityKeyInfo.Type == ManagedIdentityKeyType.KeyGuard)
            {
                // 1) Resolve provider (must be installed via .WithMtlsProofOfPossession(), see #5483)
                var provider = _requestContext.AttestationTokenProvider
                    ?? throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        "attestation_provider_missing",
                        "[ImdsV2] KeyGuard key requires attestation, but no provider is registered.",
                        null, ManagedIdentitySource.ImdsV2, null);

                // 2) Ensure the key is RSACng (KeyGuard exposes a CNG handle on Windows)
                var rsaCng = managedIdentityKeyInfo.Key as System.Security.Cryptography.RSACng
                    ?? throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        "keyguard_requires_cng",
                        "[ImdsV2] KeyGuard attestation currently supports only RSA CNG keys on Windows.",
                        null, ManagedIdentitySource.ImdsV2, null);

                // 3) Build the attestation input
                var attestationInput = new AttestationTokenInput
                {
                    ClientId = clientId,
                    AttestationEndpoint = new Uri(normalizedEndpoint), // ensure CsrMetadata exposes this
                    KeyHandle = rsaCng.Key.Handle // keep rsaCng alive while the provider runs
                };

                // 4) Obtain the token
                var attResp = await provider(attestationInput, _requestContext.UserCancellationToken).ConfigureAwait(false);

                if (attResp == null || string.IsNullOrWhiteSpace(attResp.AttestationToken))
                {
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        "attestation_empty",
                        "[ImdsV2] Attestation provider returned an empty token.",
                        null, ManagedIdentitySource.ImdsV2, null);
                }

                attestationJwt = attResp.AttestationToken;
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

            var keyInfo = await _requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider
                .GetOrCreateKeyAsync(_requestContext.Logger, _requestContext.UserCancellationToken).ConfigureAwait(false);

            var (csr, privateKey) = _requestContext.ServiceBundle.Config.CsrFactory.Generate(keyInfo.Key, csrMetadata.ClientId, csrMetadata.TenantId, csrMetadata.CuId);

            var certificateRequestResponse = await ExecuteCertificateRequestAsync(
                csrMetadata.ClientId,
                csr,
                csrMetadata.AttestationEndpoint,
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

            var tokenType = _isMtlsPopRequested ? "mtls_pop" : "bearer";

            request.BodyParameters.Add("client_id", certificateRequestResponse.ClientId);
            request.BodyParameters.Add("grant_type", OAuth2GrantType.ClientCredentials);
            request.BodyParameters.Add("scope", resource.TrimEnd('/') + "/.default");
            request.BodyParameters.Add("token_type", tokenType);

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

        private static string NormalizeAttestationEndpoint(string rawEndpoint, ILoggerAdapter logger)
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
                return absolute.ToString();
            }

            // If it has no scheme (common service behavior returning only host)
            // prepend https:// and try again.
            if (!rawEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !rawEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = "https://" + rawEndpoint;
                if (Uri.TryCreate(candidate, UriKind.Absolute, out var httpsUri))
                {
                    logger.Info(() => $"[Managed Identity] Normalized attestation endpoint '{rawEndpoint}' -> '{candidate}'.");
                    return httpsUri.ToString();
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
                return anyUri.ToString();
            }

            logger.Warning($"[Managed Identity] Failed to normalize attestation endpoint value '{rawEndpoint}'.");
            return null;
        }
    }
}
