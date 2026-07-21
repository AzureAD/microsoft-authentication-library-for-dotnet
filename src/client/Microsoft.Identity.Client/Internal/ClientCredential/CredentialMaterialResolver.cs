// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central authority for invoking <see cref="IClientCredential.GetCredentialMaterialAsync"/>.
    /// Builds a <see cref="CredentialContext"/> from the active request, invokes
    /// <see cref="IClientCredential.GetCredentialMaterialAsync"/> exactly once, and validates
    /// the returned <see cref="CredentialMaterial"/> before handing it back to the
    /// <see cref="TokenClient"/>.
    /// </summary>
    internal static class CredentialMaterialResolver
    {
        /// <summary>
        /// Resolves credential material for the given request.
        /// </summary>
        /// <param name="credential">The credential implementation to invoke.</param>
        /// <param name="requestParams">Current authentication request parameters.</param>
        /// <param name="tokenEndpoint">Resolved token endpoint URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="CredentialMaterial"/> produced by the credential.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown when the credential/mode combination is not supported
        /// (e.g., <see cref="CredentialTransportProtocol.Mtls"/> with a secret credential).
        /// </exception>
        internal static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            AuthenticationRequestParameters requestParams,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            // Single-invocation principle (issue #5943): when the preflight in
            // MtlsPopParametersInitializer has already resolved an mTLS binding certificate
            // (stashed on requestParams.MtlsCertificate) and the credential is a certificate
            // credential — whose runtime material in mTLS mode is always (empty, cert) —
            // skip the credential roundtrip entirely. This avoids re-invoking the user's
            // certificate provider delegate at runtime. Non-certificate credentials
            // (assertion-based, etc.) require runtime invocation to produce per-request
            // material (e.g., a fresh JWT-PoP assertion), so they fall through.
            //
            // Exception: when SendCertificateOverMtls=true the bearer flow requires a
            // client_assertion JWT in the POST body in addition to the cert at the TLS
            // layer. That JWT must come from the credential at runtime (Mode=OAuth path),
            // so we do NOT short-circuit in that case.
            //
            // Invariant guarded by CertificateAndClaimsClientCredential.GetCredentialMaterialAsync:
            // every subclass of CertificateAndClaimsClientCredential must keep mTLS-mode output
            // equal to (empty, cert). Subclasses that need to override mTLS-mode behaviour
            // (e.g. add custom token-request headers) must change this short-circuit too —
            // not just override the method — or their additions will be silently dropped here.
            if (requestParams.MtlsCertificate != null
                && credential is CertificateAndClaimsClientCredential
                && requestParams.AppConfig.CertificateOptions?.SendCertificateOverMtls != true)
            {
                requestParams.RequestContext.Logger.Verbose(() =>
                    $"[CredentialMaterialResolver] Reusing preflight-resolved certificate " +
                    $"(Thumbprint={requestParams.MtlsCertificate.Thumbprint}); skipping credential roundtrip.");

                return new CredentialMaterial(
                    CollectionHelpers.GetEmptyDictionary<string, string>(),
                    requestParams.MtlsCertificate);
            }

            var context = BuildContext(requestParams, tokenEndpoint);

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(context, cancellationToken)
                .ConfigureAwait(false);

            Debug.Assert(material != null, $"Credential '{credential.GetType().Name}' returned null CredentialMaterial.");
            Debug.Assert(material.TokenRequestParameters != null, $"Credential '{credential.GetType().Name}' returned null TokenRequestParameters.");

            requestParams.RequestContext.Logger.Verbose(() => $"[CredentialMaterialResolver] Credential material " +
            $"resolved. HasCertificate={material.ResolvedCertificate != null}");

            return material;
        }

        private static CredentialContext BuildContext(
            AuthenticationRequestParameters requestParams,
            string tokenEndpoint)
        {
            return CredentialContext.Create(
                clientId: requestParams.AppConfig.ClientId,
                tokenEndpoint: tokenEndpoint,
                // Mode=Mtls only for explicit mTLS PoP. All other paths (including SendCertificateOverMtls
                // bearer where MtlsCertificate is set by preflight) use Mode=OAuth so the credential
                // produces a client_assertion JWT in the POST body.
                mode: requestParams.IsMtlsPopRequested
                    ? CredentialTransportProtocol.Mtls
                    : CredentialTransportProtocol.OAuth,
                claims: requestParams.Claims,
                clientCapabilities: requestParams.AppConfig.ClientCapabilities,
                cryptographyManager: requestParams.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager,
                // When SendCertificateOverMtls=true, the client_assertion JWT must include the x5c chain
                // so that AAD can validate the assertion against the SNI-registered certificate.
                sendX5C: requestParams.SendX5C
                    || (requestParams.AppConfig.CertificateOptions?.SendCertificateOverMtls == true),
                useSha2: requestParams.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported,
                extraClientAssertionClaims: requestParams.ExtraClientAssertionClaims,
                clientAssertionFmiPath: requestParams.ClientAssertionFmiPath,
                authority: requestParams.AuthorityManager.Authority.AuthorityInfo.CanonicalAuthority?.ToString(),
                tenantId: requestParams.AuthorityManager.Authority.TenantId,
                correlationId: requestParams.RequestContext.CorrelationId,
                logger: requestParams.RequestContext.Logger,
                otelTagsEnricher: requestParams.OtelTagsEnricher);
        }
    }
}
