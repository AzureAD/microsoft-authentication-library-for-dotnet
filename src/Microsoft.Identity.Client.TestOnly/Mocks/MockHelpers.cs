// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.OAuth2.Throttling;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// Factory methods for common <see cref="MockHttpMessageHandler"/> configurations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers cover the most frequently needed mock scenarios for managed identity and mTLS PoP tests.
    /// Add the returned handler(s) to a <see cref="MockHttpManager"/> in the order MSAL is expected to call them.
    /// </para>
    /// <example>
    /// <code>
    /// var httpManager = new MockHttpManager();
    /// httpManager.AddMockHandler(MockHelpers.CreateMsiTokenHandler("my-access-token"));
    ///
    /// var app = ManagedIdentityApplicationBuilder
    ///     .Create(ManagedIdentityId.SystemAssigned)
    ///     .WithHttpManager(httpManager)
    ///     .Build();
    /// </code>
    /// </example>
    /// </remarks>
    public static class MockHelpers
    {
        // ── Response helpers ────────────────────────────────────────────────

        /// <summary>
        /// Creates a successful <see cref="HttpResponseMessage"/> with the given JSON body.
        /// </summary>
        /// <param name="successResponse">The JSON string to use as the response body.</param>
        public static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(successResponse)
            };
        }

        // ── MSI token helpers ───────────────────────────────────────────────

        /// <summary>
        /// Creates a mock handler that responds to a managed-identity (IMDS) token request
        /// with a bearer token.
        /// </summary>
        /// <param name="accessToken">The access token string to embed in the response.</param>
        /// <param name="resource">
        /// The resource audience (default: <c>https://management.azure.com/</c>).
        /// </param>
        /// <param name="expiresIn">Lifetime of the token in seconds (default: 3 599).</param>
        public static MockHttpMessageHandler CreateMsiTokenHandler(
            string accessToken,
            string resource = "https://management.azure.com/",
            int expiresIn = 3599)
        {
            string expiresOn = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
                .ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);

            string json = $"{{" +
                $"\"access_token\":\"{accessToken}\"," +
                $"\"expires_on\":\"{expiresOn}\"," +
                $"\"resource\":\"{resource}\"," +
                $"\"token_type\":\"Bearer\"," +
                $"\"client_id\":\"client_id\"" +
                $"}}";

            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = CreateSuccessResponseMessage(json)
            };
        }

        // ── Instance discovery ─────────────────────────────────────────────

        /// <summary>
        /// Creates a mock handler that responds to an AAD instance-discovery request
        /// with standard metadata for the well-known <c>login.microsoftonline.com</c> environment.
        /// </summary>
        /// <param name="discoveryEndpoint">
        /// The URL that will be matched (optional; leave <see langword="null"/> to match any URL).
        /// </param>
        public static MockHttpMessageHandler CreateInstanceDiscoveryMockHandler(
            string discoveryEndpoint = "https://login.microsoftonline.com/common/discovery/instance")
        {
            return new MockHttpMessageHandler
            {
                ExpectedUrl = discoveryEndpoint,
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = CreateSuccessResponseMessage(DiscoveryJsonResponse)
            };
        }

        // ── Client credential helpers ──────────────────────────────────────

        /// <summary>
        /// Creates a mock handler that responds to a client-credentials token request.
        /// </summary>
        /// <param name="token">The access token string (default: <c>header.payload.signature</c>).</param>
        /// <param name="tokenType">The token type (default: <c>Bearer</c>).</param>
        /// <param name="expiresIn">Lifetime in seconds (default: 3 599).</param>
        public static MockHttpMessageHandler CreateClientCredentialTokenHandler(
            string token = "header.payload.signature",
            string tokenType = "Bearer",
            int expiresIn = 3599)
        {
            string json = $"{{" +
                $"\"token_type\":\"{tokenType}\"," +
                $"\"expires_in\":{expiresIn}," +
                $"\"access_token\":\"{token}\"" +
                $"}}";

            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateSuccessResponseMessage(json)
            };
        }

        // ── Managed identity mTLS PoP helpers ──────────────────────────────

        /// <summary>
        /// Creates a mock handler for the IMDS v2 CSR-metadata endpoint
        /// (<c>/metadata/identity/getplatformmetadata</c>).
        /// </summary>
        /// <param name="statusCode">HTTP status to return (default: <c>200 OK</c>).</param>
        /// <param name="responseServerHeader">Value for the <c>server</c> response header.</param>
        /// <param name="userAssignedIdentityId">
        /// Type of user-assigned identity query parameter. Use <see cref="UserAssignedIdentityId.None"/>
        /// for system-assigned (default).
        /// </param>
        /// <param name="userAssignedId">The user-assigned identity value (client ID, resource ID, or object ID).</param>
        public static MockHttpMessageHandler MockCsrResponse(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string responseServerHeader = "IMDS/150.870.65.1854",
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            var expectedQueryParams = new Dictionary<string, string>
            {
                ["cred-api-version"] = "2.0",
            };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var param = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);
                if (param.HasValue)
                {
                    expectedQueryParams[param.Value.Key] = param.Value.Value;
                }
            }

            string content =
                "{" +
                "\"cuId\": { \"vmId\": \"fake_vmId\" }," +
                $"\"clientId\": \"{ManagedIdentityTestConstants.SystemAssignedManagedIdentityClientId}\"," +
                $"\"tenantId\": \"{ManagedIdentityTestConstants.TestTenantId}\"," +
                "\"attestationEndpoint\": \"https://fake_attestation_endpoint\"" +
                "}";

            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{ImdsV2ManagedIdentitySource.CsrMetadataPath}",
                ExpectedMethod = HttpMethod.Get,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = new Dictionary<string, string> { ["Metadata"] = "true" },
                PresentRequestHeaders = new List<string> { "x-ms-correlation-id" },
                ResponseMessage = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content)
                }
            };

            if (responseServerHeader != null)
            {
                handler.ResponseMessage.Headers.TryAddWithoutValidation("server", responseServerHeader);
            }

            return handler;
        }

        /// <summary>
        /// Creates a mock handler for the IMDS v2 credential-issuance endpoint
        /// (<c>/metadata/identity/issuecredential</c>).
        /// </summary>
        /// <param name="userAssignedIdentityId">Type of user-assigned identity (default: system-assigned).</param>
        /// <param name="userAssignedId">The user-assigned identity value.</param>
        /// <param name="certificate">
        /// The DER-encoded base-64 certificate string to embed in the response.
        /// Defaults to <see cref="ManagedIdentityTestConstants.ValidTestCertificate"/>.
        /// </param>
        public static MockHttpMessageHandler MockCertificateRequestResponse(
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string certificate = null)
        {
            certificate ??= ManagedIdentityTestConstants.ValidTestCertificate;

            var expectedQueryParams = new Dictionary<string, string>
            {
                ["cred-api-version"] = ImdsV2ManagedIdentitySource.ImdsV2ApiVersion
            };

            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                var param = ImdsManagedIdentitySource.GetUserAssignedIdQueryParam(
                    (ManagedIdentityIdType)userAssignedIdentityId, userAssignedId, null);
                if (param.HasValue)
                {
                    expectedQueryParams[param.Value.Key] = param.Value.Value;
                }
            }

            string content =
                "{" +
                $"\"client_id\": \"{ManagedIdentityTestConstants.SystemAssignedManagedIdentityClientId}\"," +
                $"\"tenant_id\": \"{ManagedIdentityTestConstants.TestTenantId}\"," +
                $"\"certificate\": \"{certificate}\"," +
                "\"identity_type\": \"fake_identity_type\"," +
                $"\"mtls_authentication_endpoint\": \"{ManagedIdentityTestConstants.MtlsAuthenticationEndpoint}\"" +
                "}";

            return new MockHttpMessageHandler
            {
                ExpectedUrl = $"{ImdsManagedIdentitySource.DefaultImdsBaseEndpoint}{ImdsV2ManagedIdentitySource.CertificateRequestPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedQueryParams = expectedQueryParams,
                ExpectedRequestHeaders = new Dictionary<string, string> { ["Metadata"] = "true" },
                PresentRequestHeaders = new List<string> { "x-ms-correlation-id" },
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                }
            };
        }

        /// <summary>
        /// Creates a mock handler for the IMDS v2 Entra token endpoint
        /// (<c>/oauth2/v2.0/token</c> on the mTLS authentication endpoint).
        /// </summary>
        /// <remarks>
        /// This handler validates that MSAL sends <c>token_type=mtls_pop</c> in the request body.
        /// </remarks>
        public static MockHttpMessageHandler MockImdsV2EntraTokenRequestResponse()
        {
            // Build expected MSAL ID headers dynamically so they stay in sync with the running MSAL version.
            var expectedRequestHeaders = new Dictionary<string, string>
            {
                [ThrottleCommon.ThrottleRetryAfterHeaderName] = ThrottleCommon.ThrottleRetryAfterHeaderValue
            };

            var idParams = MsalIdHelper.GetMsalIdParameters(null);
            foreach (var kvp in idParams)
            {
                expectedRequestHeaders[kvp.Key] = kvp.Value;
            }

            return new MockHttpMessageHandler
            {
                ExpectedUrl = $"{ManagedIdentityTestConstants.MtlsAuthenticationEndpoint}/{ManagedIdentityTestConstants.TestTenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["token_type"] = "mtls_pop" },
                ExpectedRequestHeaders = expectedRequestHeaders,
                PresentRequestHeaders = new List<string> { "x-ms-correlation-id" },
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetMsiSuccessfulResponse(imdsV2: true))
                }
            };
        }

        // ── Internal helpers ───────────────────────────────────────────────

        internal static string GetMsiSuccessfulResponse(int expiresInHours = 1, bool imdsV2 = false)
        {
            string expiresOnKey = imdsV2 ? "expires_in" : "expires_on";
            string expiresOnValue = DateTimeOffset.UtcNow.AddHours(expiresInHours)
                .ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);
            string tokenType = imdsV2 ? "mtls_pop" : "Bearer";

            return $"{{" +
                $"\"access_token\":\"{ManagedIdentityTestConstants.TestAccessToken}\"," +
                $"\"{expiresOnKey}\":\"{expiresOnValue}\"," +
                $"\"resource\":\"https://management.azure.com/\"," +
                $"\"token_type\":\"{tokenType}\"," +
                $"\"client_id\":\"client_id\"" +
                $"}}";
        }

        // ── Private constants ──────────────────────────────────────────────

        private const string DiscoveryJsonResponse =
            @"{
                ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                ""api-version"":""1.1"",
                ""metadata"":[
                    {
                    ""preferred_network"":""login.microsoftonline.com"",
                    ""preferred_cache"":""login.windows.net"",
                    ""aliases"":[
                        ""login.microsoftonline.com"",
                        ""login.windows.net"",
                        ""login.microsoft.com"",
                        ""sts.windows.net""]},
                    {
                    ""preferred_network"":""login.partner.microsoftonline.cn"",
                    ""preferred_cache"":""login.partner.microsoftonline.cn"",
                    ""aliases"":[
                        ""login.partner.microsoftonline.cn"",
                        ""login.chinacloudapi.cn""]},
                    {
                    ""preferred_network"":""login.microsoftonline.de"",
                    ""preferred_cache"":""login.microsoftonline.de"",
                    ""aliases"":[
                            ""login.microsoftonline.de""]},
                    {
                    ""preferred_network"":""login.microsoftonline.us"",
                    ""preferred_cache"":""login.microsoftonline.us"",
                    ""aliases"":[
                        ""login.microsoftonline.us"",
                        ""login.usgovcloudapi.net""]},
                    {
                    ""preferred_network"":""login-us.microsoftonline.com"",
                    ""preferred_cache"":""login-us.microsoftonline.com"",
                    ""aliases"":[
                        ""login-us.microsoftonline.com""]}
                ]
            }";
    }
}
