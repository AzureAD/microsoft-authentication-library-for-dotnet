// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
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
            var queryParams = ImdsV2QueryParamsHelper(requestContext);

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { "x-ms-client-request-id", requestContext.CorrelationId.ToString() }
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
        }

        private static void ThrowProbeFailedException(
            String errorMessage,
            Exception ex = null,
            int? statusCode = null)
        {
            throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                MsalError.ManagedIdentityRequestFailed,
                $"[ImdsV2] ${errorMessage}",
                ex,
                ManagedIdentitySource.ImdsV2,
                statusCode);
        }

        private static bool ValidateCsrMetadataResponse(
            HttpResponse response,
            ILoggerAdapter logger,
            bool probeMode)
        {
            /*
             * Match "IMDS/" at start of "server" header string (`^IMDS\/`)
             * Match the first three numbers with dots (`\d+.\d+.\d+.`)
             * Capture the last number in a group (`(\d+)`)
             * Ensure end of string (`$`)
             *
             * Example:
             * [
             * "IMDS/150.870.65.1556",  // index 0: full match
             * "1556"                   // index 1: captured group (\d+)
             * ]
             */
            string serverHeader = response.HeadersAsDictionary.TryGetValue("server", out var value) ? value : null;
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

            var match = System.Text.RegularExpressions.Regex.Match(
                serverHeader,
                @"^IMDS/\d+\.\d+\.\d+\.(\d+)$"
            );
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int version) || version < 1854)
            {
                if (probeMode)
                {
                    logger.Info(() => $"[Managed Identity] IMDSv2 managed identity is not available. 'server' header format/version invalid. Extracted version: {match.Groups[1].Value}");
                    return false;
                }
                else
                {
                    ThrowProbeFailedException(
                        $"ImdsV2ManagedIdentitySource.GetCsrMetadataAsync failed because the 'server' header format/version invalid. Extracted version: {match.Groups[1].Value}. Status code: {response.StatusCode} Body: {response.Body}",
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
            base(requestContext, ManagedIdentitySource.ImdsV2) { }

        private async Task<CertificateRequestResponse> ExecuteCertificateRequestAsync(string csr)
        {
            var queryParams = ImdsV2QueryParamsHelper(_requestContext);

            // TODO: add bypass_cache query param in case of token revocation. Boolean: true/false

            var headers = new Dictionary<string, string>
            {
                { "Metadata", "true" },
                { "x-ms-client-request-id", _requestContext.CorrelationId.ToString() }
            };
            
            var body = $"{{\"csr\":\"{csr}\"}}";

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
            var (csr, privateKey) = _requestContext.ServiceBundle.Config.CsrFactory.Generate(csrMetadata.ClientId, csrMetadata.TenantId, csrMetadata.CuId);

            var certificateRequestResponse = await ExecuteCertificateRequestAsync(csr).ConfigureAwait(false);
            
            // transform certificateRequestResponse.Certificate to x509 with private key
            var mtlsCertificate = AttachPrivateKeyToCert(
                certificateRequestResponse.Certificate,
                privateKey);

            ManagedIdentityRequest request = new(HttpMethod.Post, new Uri($"{certificateRequestResponse.MtlsAuthenticationEndpoint}/{certificateRequestResponse.TenantId}{AcquireEntraTokenPath}"));
            request.Headers.Add("x-ms-client-request-id", _requestContext.CorrelationId.ToString());
            request.BodyParameters.Add("client_id", certificateRequestResponse.ClientId);
            request.BodyParameters.Add("grant_type", certificateRequestResponse.Certificate);
            request.BodyParameters.Add("scope", "https://management.azure.com/.default");
            request.RequestType = RequestType.Imds;
            request.MtlsCertificate = mtlsCertificate;

            return request;
        }

        /// <summary>
        /// Attaches a private key to a certificate for use in mTLS authentication.
        /// </summary>
        /// <param name="certificatePem">The certificate in PEM format</param>
        /// <param name="privateKey">The RSA private key to attach</param>
        /// <returns>An X509Certificate2 with the private key attached</returns>
        /// <exception cref="ArgumentNullException">Thrown when certificatePem or privateKey is null</exception>
        /// <exception cref="ArgumentException">Thrown when certificatePem is not a valid PEM certificate</exception>
        /// <exception cref="FormatException">Thrown when the certificate cannot be parsed</exception>
        internal X509Certificate2 AttachPrivateKeyToCert(string certificatePem, RSA privateKey)
        {
            if (string.IsNullOrEmpty(certificatePem))
                throw new ArgumentNullException(nameof(certificatePem));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            X509Certificate2 certificate;

#if NET8_0_OR_GREATER
            // .NET 8.0+ has direct PEM parsing support
            certificate = X509Certificate2.CreateFromPem(certificatePem);
            // Attach the private key and return a new certificate instance
            return certificate.CopyWithPrivateKey(privateKey);
#else
            // .NET Framework 4.7.2 and .NET Standard 2.0 - manual PEM parsing and private key attachment
            certificate = ParseCertificateFromPem(certificatePem);
            return AttachPrivateKeyToOlderFrameworks(certificate, privateKey);
#endif
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Parses a certificate from PEM format for older .NET versions.
        /// </summary>
        /// <param name="certificatePem">The certificate in PEM format</param>
        /// <returns>An X509Certificate2 instance</returns>
        /// <exception cref="ArgumentException">Thrown when the PEM format is invalid</exception>
        /// <exception cref="FormatException">Thrown when the Base64 content cannot be decoded</exception>
        internal static X509Certificate2 ParseCertificateFromPem(string certificatePem)
        {
            const string CertBeginMarker = "-----BEGIN CERTIFICATE-----";
            const string CertEndMarker = "-----END CERTIFICATE-----";

            int startIndex = certificatePem.IndexOf(CertBeginMarker, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                throw new ArgumentException("Invalid PEM format: missing BEGIN CERTIFICATE marker", nameof(certificatePem));
            }

            startIndex += CertBeginMarker.Length;
            int endIndex = certificatePem.IndexOf(CertEndMarker, startIndex, StringComparison.Ordinal);
            if (endIndex == -1)
            {
                throw new ArgumentException("Invalid PEM format: missing END CERTIFICATE marker", nameof(certificatePem));
            }

            string base64Content = certificatePem.Substring(startIndex, endIndex - startIndex)
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "");

            if (string.IsNullOrEmpty(base64Content))
            {
                throw new ArgumentException("Invalid PEM format: no certificate content found", nameof(certificatePem));
            }

            try
            {
                byte[] certBytes = Convert.FromBase64String(base64Content);
                return new X509Certificate2(certBytes);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Invalid PEM format: certificate content is not valid Base64", ex);
            }
        }

        /// <summary>
        /// Attaches a private key to a certificate for older .NET Framework versions.
        /// This method uses the older RSACng approach for .NET Framework 4.7.2 and .NET Standard 2.0.
        /// </summary>
        /// <param name="certificate">The certificate without private key</param>
        /// <param name="privateKey">The RSA private key to attach</param>
        /// <returns>An X509Certificate2 with the private key attached</returns>
        /// <exception cref="NotSupportedException">Thrown when private key attachment fails</exception>
        internal X509Certificate2 AttachPrivateKeyToOlderFrameworks(X509Certificate2 certificate, RSA privateKey)
        {
            try
            {
                // For older frameworks, we need to use the legacy approach with RSACryptoServiceProvider
                // First, export the RSA parameters from the provided private key
                var parameters = privateKey.ExportParameters(includePrivateParameters: true);
                
                // Create a new RSACryptoServiceProvider with the correct key size
                int keySize = parameters.Modulus.Length * 8;
                var rsaProvider = new RSACryptoServiceProvider(keySize);
                
                try
                {
                    // Import the parameters into the new provider
                    rsaProvider.ImportParameters(parameters);
                    
                    // Create a new certificate instance from the raw data
                    var certWithPrivateKey = new X509Certificate2(certificate.RawData);
                    
                    // Assign the private key using the legacy property
                    certWithPrivateKey.PrivateKey = rsaProvider;
                    
                    return certWithPrivateKey;
                }
                catch
                {
                    // Clean up the RSA provider if something goes wrong
                    rsaProvider?.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(
                    "Failed to attach private key to certificate on this .NET Framework version.", ex);
            }
        }
#endif

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
    }
}
