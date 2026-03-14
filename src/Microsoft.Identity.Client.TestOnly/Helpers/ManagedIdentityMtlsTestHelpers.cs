// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// High-level facade for setting up mock HTTP handlers for managed identity mTLS PoP token acquisition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods encapsulate the three-step IMDS v2 flow:
    /// <list type="number">
    ///   <item><description>
    ///     <b>CSR Metadata</b> (GET <c>/metadata/identity/getplatformmetadata</c>) —
    ///     returns platform metadata including attestation endpoint and identity info.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Certificate Request</b> (POST <c>/metadata/identity/issuecredential</c>) —
    ///     issues a signed binding certificate.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Entra Token</b> (POST <c>{mtls_endpoint}/{tenantId}/oauth2/v2.0/token</c>) —
    ///     acquires an mTLS-bound access token.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class ManagedIdentityMtlsTestHelpers
    {
        /// <summary>
        /// Adds the three mock handlers needed for a full managed-identity mTLS PoP token acquisition.
        /// </summary>
        /// <param name="httpManager">The <see cref="MockHttpManager"/> to add handlers to.</param>
        /// <param name="userAssignedIdentityId">
        /// Specifies the UAMI identifier type. Use <see cref="UserAssignedIdentityId.None"/> (default)
        /// for system-assigned managed identity.
        /// </param>
        /// <param name="userAssignedId">
        /// The user-assigned identity value (client ID, resource ID, or object ID).
        /// Must be provided when <paramref name="userAssignedIdentityId"/> is not <see cref="UserAssignedIdentityId.None"/>.
        /// </param>
        /// <returns>The same <paramref name="httpManager"/> for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// using var httpManager = new MockHttpManager();
        ///
        /// httpManager.AddManagedIdentityMtlsTokenMocks(
        ///     userAssignedIdentityId: UserAssignedIdentityId.ClientId,
        ///     userAssignedId: "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a");
        ///
        /// var app = ManagedIdentityApplicationBuilder
        ///     .Create(ManagedIdentityId.WithUserAssignedClientId("04ca4d6a-c720-4ba1-aa06-f6634b73fe7a"))
        ///     .WithHttpManager(httpManager)
        ///     .Build();
        ///
        /// var result = await app
        ///     .AcquireTokenForManagedIdentity("https://management.azure.com/.default")
        ///     .WithMtlsProofOfPossession()
        ///     .ExecuteAsync();
        ///
        /// Assert.NotNull(result.BindingCertificate);
        /// Assert.Equal("mtls_pop", result.TokenType);
        /// </code>
        /// </example>
        public static MockHttpManager AddManagedIdentityMtlsTokenMocks(
            this MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            // 1 — CSR metadata
            httpManager.AddMockHandler(
                MockHelpers.MockCsrResponse(
                    userAssignedIdentityId: userAssignedIdentityId,
                    userAssignedId: userAssignedId));

            // 2 — Certificate issuance
            httpManager.AddMockHandler(
                MockHelpers.MockCertificateRequestResponse(
                    userAssignedIdentityId: userAssignedIdentityId,
                    userAssignedId: userAssignedId));

            // 3 — Entra token
            httpManager.AddMockHandler(
                MockHelpers.MockImdsV2EntraTokenRequestResponse());

            return httpManager;
        }

        /// <summary>
        /// Adds the two mock handlers needed for an mTLS PoP token acquisition when a valid binding
        /// certificate is already cached from a prior call.
        /// </summary>
        /// <remarks>
        /// When a certificate is cached, MSAL skips the certificate-issuance step and calls only
        /// the CSR-metadata endpoint followed by the Entra token endpoint.
        /// </remarks>
        /// <param name="httpManager">The <see cref="MockHttpManager"/> to add handlers to.</param>
        /// <param name="userAssignedIdentityId">
        /// Specifies the UAMI identifier type. Use <see cref="UserAssignedIdentityId.None"/> (default)
        /// for system-assigned managed identity.
        /// </param>
        /// <param name="userAssignedId">
        /// The user-assigned identity value when <paramref name="userAssignedIdentityId"/> is not None.
        /// </param>
        /// <returns>The same <paramref name="httpManager"/> for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// // First acquisition mints the certificate
        /// httpManager.AddManagedIdentityMtlsTokenMocks();
        /// var result1 = await app.AcquireTokenForManagedIdentity(scope)
        ///     .WithMtlsProofOfPossession()
        ///     .ExecuteAsync();
        ///
        /// // Force-refresh: certificate is cached, so only two HTTP calls are made
        /// httpManager.AddManagedIdentityMtlsTokenMocks_CachedCertRefresh();
        /// var result2 = await app.AcquireTokenForManagedIdentity(scope)
        ///     .WithForceRefresh(true)
        ///     .WithMtlsProofOfPossession()
        ///     .ExecuteAsync();
        ///
        /// Assert.Equal(result1.BindingCertificate.Thumbprint, result2.BindingCertificate.Thumbprint);
        /// </code>
        /// </example>
        public static MockHttpManager AddManagedIdentityMtlsTokenMocks_CachedCertRefresh(
            this MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null)
        {
            // 1 — CSR metadata (still called even on refresh)
            httpManager.AddMockHandler(
                MockHelpers.MockCsrResponse(
                    userAssignedIdentityId: userAssignedIdentityId,
                    userAssignedId: userAssignedId));

            // 2 — Entra token (no certificate issuance because certificate is cached)
            httpManager.AddMockHandler(
                MockHelpers.MockImdsV2EntraTokenRequestResponse());

            return httpManager;
        }
    }
}
