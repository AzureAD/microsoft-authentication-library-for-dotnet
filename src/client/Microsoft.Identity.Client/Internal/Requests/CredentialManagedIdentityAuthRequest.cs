// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ManagedIdentity;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Implements MSI V2 token acquisition flow for VM/VMSS using the `/issuecredential` endpoint.
    /// This request uses a short-lived binding certificate to perform mTLS authentication against ESTS.
    ///
    /// Flow Overview:
    ///   1. Call getPlatformMetadata to retrieve tenant_id, client_id (UAID), CUID, and MAA endpoint.
    ///   2. Generate or load key material and build a CSR (with CUID attribute).
    ///   3. If attestation is required (attestable CU), obtain attestation_token from MAA.
    ///   4. Call /issuecredential endpoint with CSR (+ attestation_token if applicable) to obtain:
    ///        - Binding certificate (valid ~7 days)
    ///        - Regional token endpoint URL
    ///   5. Perform mTLS token request to ESTS regional endpoint to acquire access token.
    ///   6. Cache and return AuthenticationResult (access token + cert if needed by caller).
    /// </summary>
    internal sealed class CredentialManagedIdentityAuthRequest : ManagedIdentityAuthRequestBase
    {
        internal const string IdentityUnavailableError = "[Managed Identity] Authentication unavailable. Either the requested identity has not been assigned to this resource, or other errors could be present. See inner exception.";
        internal const string GatewayError = "[Managed Identity] Authentication unavailable. The request failed due to a gateway error.";

        public CredentialManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
        }

        /// <summary>
        /// Main entry point for MSI V2 token acquisition.
        /// </summary>
        protected override async Task<AuthenticationResult> SendTokenRequestAsync(
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            Exception exception = null;
            string message = string.Empty;

            try
            {
                //
                // STEP 1: Retrieve platform metadata
                //   - Endpoint: GET /metadata/identity/getPlatformMetadata?api-version=2025-05-01
                //   - Required headers: Metadata=true
                //   - Returns: UAID (client_id), tenant_id, CUID, MAA endpoint (if attestable)
                //
                ManagedIdentityMetadataResponse metadata =
                    await GetMetaDataAsync().ConfigureAwait(false);

                //
                // STEP 2: Generate or load key & build CSR
                //   - CSR subject: CN={client_id}, DC={tenant_id}
                //   - Attribute OID 1.2.840.113549.1.9.7 = CUID (PrintableString)
                //   - Signed with: RSA 2048
                //   - Durable key if from KeyGuard KSP (Windows attested)
                //
                //   TODO: Implement KeyStore selection based on OS and attestation capability.
                //

                //
                // STEP 3: (Optional) Obtain attestation token
                //   - Required for attested compute units (KeyGuard)
                //   - POST to MAA /attest/keyguard endpoint with key info
                //   - Skip for unattested flows
                //   - Next Commit will have this implemented. (Owner - Gladwin) 

                //
                // STEP 4: Call /issuecredential endpoint
                //   - POST /metadata/identity/issuecredential?cid={CUID}&uaid={client_id}&api-version=2025-05-01
                //   - Body: { "csr": "<Base64 CSR>", "attestation_token": "<jwt>"? }
                //   - Returns: client_credential (Base64 DER cert), regional_token_url
                //
                ManagedIdentityCredentialResponse credentialResponse =
                    await GetCredentialCertificateAsync().ConfigureAwait(false);

                var bindingCert = new X509Certificate2(
                    credentialResponse.CertificateForMtls,
                    (string)null,
                    X509KeyStorageFlags.MachineKeySet);

                //
                // STEP 5: Build OAuth2 client for mTLS token request
                //
                OAuth2Client mtlsClient = CreateMtlsClientRequest(
                    AuthenticationRequestParameters.RequestContext.ServiceBundle.HttpManager,
                    credentialResponse,
                    bindingCert);

                //
                // STEP 6: Perform mTLS token request to ESTS
                //   - Endpoint: {regional_token_url}/{tenant_id}/oauth2/v2.0/token
                //   - grant_type=client_credentials
                //   - scope=.../.default
                //   - token_type=mtls_pop for attested flows, default bearer otherwise
                //
                Uri tokenUrl = new Uri(credentialResponse.RegionalTokenUrl); // from /issuecredential

                MsalTokenResponse msalTokenResponse = await mtlsClient.GetTokenAsync(
                        tokenUrl,
                        AuthenticationRequestParameters.RequestContext,
                        true,
                        AuthenticationRequestParameters.OnBeforeTokenRequestHandler)
                    .ConfigureAwait(false);

                msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

                logger.Info("[CredentialManagedIdentityAuthRequest] Successfully acquired token via MSI V2 mTLS flow.");

                //
                // STEP 7: Cache and return AuthenticationResult
                //
                return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error($"[CredentialManagedIdentityAuthRequest] Exception: {ex}");
                message = IdentityUnavailableError;
                exception = ex;

                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    message,
                    exception,
                    ManagedIdentitySource.Credential,
                    null);
            }
        }

        /// <summary>
        /// Calls getPlatformMetadata endpoint.
        /// TODO: Implement real HTTP call.
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<ManagedIdentityMetadataResponse> GetMetaDataAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Placeholder: populate with actual HTTP call to IMDS getPlatformMetadata endpoint.
            return new ManagedIdentityMetadataResponse
            {
                ClientId = "TODO",
                TenantId = "TODO",
                //Other properties
            };
        }

        /// <summary>
        /// Calls /issuecredential endpoint with CSR (+ attestation token if applicable).
        /// TODO: Implement CSR generation, attestation handling, and HTTP call.
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<ManagedIdentityCredentialResponse> GetCredentialCertificateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Placeholder: populate with actual HTTP call to /issuecredential endpoint.
            return new ManagedIdentityCredentialResponse
            {
                CertificateForMtls = Array.Empty<byte>(),
                ClientId = "TODO",
                RegionalTokenUrl = "TODO"
            };
        }

        private OAuth2Client CreateMtlsClientRequest(
            IHttpManager httpManager,
            ManagedIdentityCredentialResponse credentialResponse,
            X509Certificate2 x509Certificate2)
        {
            var client = new OAuth2Client(
                AuthenticationRequestParameters.RequestContext.Logger,
                httpManager,
                x509Certificate2);

            // Ensure scope ends with /.default for client_credential flows
            string scopes = AuthenticationRequestParameters.Scope.AsSingleString();
            if (!scopes.EndsWith("/.default", StringComparison.OrdinalIgnoreCase))
            {
                scopes += "/.default";
            }

            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials);
            client.AddBodyParameter(OAuth2Parameter.Scope, scopes);
            client.AddBodyParameter(OAuth2Parameter.ClientId, credentialResponse.ClientId);

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                client.AddBodyParameter(OAuth2Parameter.Claims, AuthenticationRequestParameters.ClaimsAndClientCapabilities);
            }

            // TODO: Add token_type=mtls_pop requested.
            return client;
        }
    }
}
