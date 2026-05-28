// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    /// <summary>
    /// Encapsulates the mTLS/PoP initialization logic for token requests.
    /// Keeps AcquireTokenCommonParameters lean and makes the init logic testable in isolation.
    /// </summary>
    internal static class MtlsPopParametersInitializer
    {
        internal static async Task TryInitAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            if (p.IsMtlsPopRequested)
            {
                await InitExplicitMtlsPopAsync(p, serviceBundle, ct).ConfigureAwait(false);
                return;
            }

            await TryInitImplicitBearerOverMtlsAsync(p, serviceBundle, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// NON-PoP request: we may still need mTLS transport in two situations.
        /// <list type="number">
        ///   <item><description>The app-level <see cref="AppConfig.CertificateOptions.SendCertificateOverMtls"/> option
        ///         is set on a certificate-based credential — resolved polymorphically through
        ///         <see cref="IClientCredential.GetCredentialMaterialAsync"/> in mTLS mode.</description></item>
        ///   <item><description>The credential is a signed-assertion provider that opportunistically returns a
        ///         <see cref="ClientSignedAssertion.TokenBindingCertificate"/>. This path stays on the
        ///         pre-existing <see cref="IClientSignedAssertionProvider"/> capability because the
        ///         semantics are best-effort (no throw when the cert is absent).</description></item>
        /// </list>
        /// </summary>
        private static async Task TryInitImplicitBearerOverMtlsAsync(
            AcquireTokenCommonParameters tokenParameters,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            if (tokenParameters.MtlsCertificate != null)
            {
                return;
            }

            // Case 1 – App opted into mTLS Bearer via SendCertificateOverMtls.
            //          Resolve the cert polymorphically; non-certificate credentials throw and we leave
            //          MtlsCertificate unset (the caller will go down the regular Bearer path).
            if (serviceBundle.Config.CertificateOptions?.SendCertificateOverMtls == true)
            {
                CredentialMaterial material = await ResolveMtlsMaterialAsync(
                    tokenParameters, serviceBundle, ct).ConfigureAwait(false);

                tokenParameters.MtlsCertificate = material.ResolvedCertificate;
                return;
            }

            // Case 2 – Signed-assertion provider may opportunistically return a binding cert.
            //          Kept on the capability interface because we do not want to throw when no
            //          cert is supplied; GetCredentialMaterialAsync(Mtls) would.
            if (serviceBundle.Config.ClientCredential is IClientSignedAssertionProvider signedProvider)
            {
                AssertionRequestOptions opts = CreateAssertionRequestOptions(tokenParameters, serviceBundle, ct);

                ClientSignedAssertion ar =
                    await signedProvider.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar?.TokenBindingCertificate != null)
                {
                    tokenParameters.MtlsCertificate = ar.TokenBindingCertificate;
                }
            }
        }

        /// <summary>
        /// EXPLICIT mTLS PoP requested: resolve the binding certificate polymorphically and initialize
        /// PoP parameters (auth scheme + cert + tenanted-authority check).
        /// </summary>
        /// <remarks>
        /// Every credential answers the same polymorphic question via
        /// <see cref="IClientCredential.GetCredentialMaterialAsync"/> in mTLS mode:
        /// <list type="bullet">
        ///   <item><description>Certificate credentials (static + dynamic) skip JWT signing and return
        ///         <c>(empty, cert)</c>.</description></item>
        ///   <item><description>Signed-assertion credentials invoke their delegate and return
        ///         <c>(jwt-pop, cert)</c>, or throw <see cref="MsalError.MtlsCertificateNotProvided"/>
        ///         if no <see cref="ClientSignedAssertion.TokenBindingCertificate"/> is supplied.</description></item>
        ///   <item><description>Client-secret / static-assertion / string-callback credentials throw
        ///         <see cref="MsalError.InvalidCredentialMaterial"/> via
        ///         <see cref="ClientCredentialGuards.ThrowIfMtlsNotSupported"/>; we translate that
        ///         to the public <see cref="MsalError.MtlsCertificateNotProvided"/> code below.</description></item>
        /// </list>
        /// No concrete-credential downcasts — addresses reviewer feedback on PR #5957.
        /// </remarks>
        private static async Task InitExplicitMtlsPopAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            CredentialMaterial material;
            try
            {
                material = await ResolveMtlsMaterialAsync(p, serviceBundle, ct).ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == MsalError.InvalidCredentialMaterial)
            {
                // Credential layer reports "this credential cannot produce material in mTLS mode" with
                // InvalidCredentialMaterial. The public mTLS PoP API surface has historically returned
                // MtlsCertificateNotProvided for the same misconfiguration — preserve that contract so
                // callers that match on ex.ErrorCode keep working.
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage,
                    ex);
            }

            // Every supported credential returns a non-null cert in mTLS mode or throws. A null here
            // would indicate a credential that violated the Mode contract.
            if (material.ResolvedCertificate is null)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }

            await InitMtlsPopParametersAsync(p, material.ResolvedCertificate, serviceBundle, ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Single polymorphic entry point for resolving an mTLS binding certificate from any
        /// <see cref="IClientCredential"/>. Builds the preflight <see cref="CredentialContext"/>
        /// and asks the credential for its material in <see cref="CredentialTransportProtocol.Mtls"/>
        /// mode.
        /// </summary>
        private static Task<CredentialMaterial> ResolveMtlsMaterialAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            CredentialContext ctx = BuildPreflightContext(
                p, serviceBundle, CredentialTransportProtocol.Mtls);

            return serviceBundle.Config.ClientCredential.GetCredentialMaterialAsync(ctx, ct);
        }

        /// <summary>
        /// Builds a best-effort <see cref="CredentialContext"/> for preflight, mirroring
        /// <see cref="CredentialMaterialResolver.BuildContext"/> for the fields that are known
        /// before runtime authority resolution. Fields used only for JWT signing (TokenEndpoint /
        /// UseSha2 / SendX5C) fall back to app-level configuration — credentials operating in
        /// <see cref="CredentialTransportProtocol.Mtls"/> mode do not depend on them.
        /// </summary>
        private static CredentialContext BuildPreflightContext(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CredentialTransportProtocol mode)
        {
            AuthorityInfo authorityInfo = serviceBundle.Config.Authority?.AuthorityInfo;
            string canonicalAuthority = authorityInfo?.CanonicalAuthority?.AbsoluteUri;

            // GetFirstPathSegment throws for non-AAD shapes; only set TenantId for AAD here.
            string tenantId = authorityInfo?.AuthorityType == AuthorityType.Aad
                ? AuthorityInfo.GetFirstPathSegment(authorityInfo.CanonicalAuthority)
                : null;

            return CredentialContext.Create(
                clientId: serviceBundle.Config.ClientId,
                tokenEndpoint: canonicalAuthority,
                mode: mode,
                claims: p.Claims,
                clientCapabilities: serviceBundle.Config.ClientCapabilities,
                cryptographyManager: serviceBundle.PlatformProxy.CryptographyManager,
                sendX5C: serviceBundle.Config.SendX5C,
                useSha2: authorityInfo?.IsSha2CredentialSupported ?? false,
                extraClientAssertionClaims: null,
                clientAssertionFmiPath: p.ClientAssertionFmiPath,
                authority: canonicalAuthority,
                tenantId: tenantId,
                correlationId: p.CorrelationId,
                logger: serviceBundle.ApplicationLogger);
        }

        private static AssertionRequestOptions CreateAssertionRequestOptions(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            return new AssertionRequestOptions
            {
                ClientID = serviceBundle.Config.ClientId,
                ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                Claims = p.Claims,
                CancellationToken = ct,
                ClientAssertionFmiPath = p.ClientAssertionFmiPath,
                CorrelationId = p.CorrelationId,

                // Best-effort context. IMPORTANT: use AbsoluteUri, not Uri.Authority (host only).
                TokenEndpoint = serviceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority.AbsoluteUri
            };
        }

        /// <summary>
        /// Enforces the mTLS PoP authority contract: when the configured authority is AAD,
        /// it must be tenanted (i.e., not /common or /organizations). Runs AFTER the credential
        /// provider so that credentials that cannot produce a certificate in mTLS mode
        /// (e.g., client-secret, client-assertion) preserve the public
        /// <see cref="MsalError.MtlsCertificateNotProvided"/> error-code contract before
        /// authority-shape errors surface. See <see cref="InitExplicitMtlsPopAsync"/>.
        /// </summary>
        private static void ValidateAadAuthorityForPop(IServiceBundle serviceBundle)
        {
            AuthorityInfo authorityInfo = serviceBundle.Config.Authority?.AuthorityInfo;
            if (authorityInfo?.AuthorityType != AuthorityType.Aad)
            {
                return;
            }

            string tenant = AuthorityInfo.GetFirstPathSegment(authorityInfo.CanonicalAuthority);
            if (AadAuthority.IsCommonOrOrganizationsTenant(tenant))
            {
                throw new MsalClientException(
                    MsalError.MissingTenantedAuthority,
                    MsalErrorMessage.MtlsNonTenantedAuthorityNotAllowedMessage);
            }
        }

        private static async Task InitMtlsPopParametersAsync(
            AcquireTokenCommonParameters p,
            X509Certificate2 cert,
            IServiceBundle serviceBundle,
            CancellationToken ct = default)
        {
            ValidateAadAuthorityForPop(serviceBundle);

            // If the current operation supports the AfterCredentialEvaluation lifecycle hook,
            // invoke it with the cert instead of replacing the operation. This enables
            // composition (e.g., CDT + mTLS POP) where the operation handles both concerns.
            if (p.AuthenticationOperation is IAuthenticationOperation3 op3)
            {
                await op3.AfterCredentialEvaluationAsync(new CredentialEvaluationContext(cert), ct).ConfigureAwait(false);
                p.MtlsCertificate = cert;
                return;
            }

            p.AuthenticationOperation = new MtlsPopAuthenticationOperation(cert);
            p.MtlsCertificate = cert;
        }
    }
}
