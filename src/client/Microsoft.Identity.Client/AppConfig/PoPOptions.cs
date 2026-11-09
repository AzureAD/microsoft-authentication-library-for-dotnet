// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Options that control mTLS Proof-of-Possession (PoP) token acquisition. This type is the
    /// extensibility point for PoP-related knobs so future settings can be added without growing
    /// the builder surface.
    /// </summary>
    /// <remarks>
    /// This type is shared by managed identity and confidential client mTLS Proof-of-Possession
    /// scenarios.
    /// </remarks>
    public class PoPOptions
    {
        /// <summary>
        /// Gets or sets a caller-owned certificate for non-attested managed identity mTLS PoP.
        /// When null, a managed identity PoP request without
        /// attestation support selects the configured Windows RDP listener certificate internally.
        /// Other requests retain their existing certificate acquisition flow.
        /// </summary>
        /// <remarks>
        /// IMDS test-endpoint support only; no experimental-feature opt-in is required.
        /// Do not combine this option with attestation support. The certificate
        /// must have an accessible RSA private key. The caller retains ownership and must keep it
        /// usable while the request, its proactive background refresh, or returned authentication
        /// result uses it. Each execution retains its own certificate reference.
        /// An explicitly supplied certificate bypasses RDP discovery. MSAL never changes private-key permissions.
        /// Automatically discovered certificates remain usable through the returned authentication result;
        /// callers do not need to load, supply, or dispose the SDK-selected certificate.
        /// A custom HTTP client factory must also implement <see cref="IMsalWsTrustHttpClientFactory"/>
        /// and return an anonymous, redirect-free client from GetHttpClient(false).
        /// Otherwise the request fails before sending, rather than replacing the configured factory.
        /// </remarks>
        public X509Certificate2 BindingCertificate { get; set; }

        /// <summary>
        /// Gets or sets the minimum binding strength the host must be able to produce for the
        /// request to succeed. This is a <b>floor assertion</b>, not a downgrade selector.
        /// The existing IMDSv2 acquisition flow uses the host's maximum binding strength.
        /// The experimental non-attested protocol supports Software strength only, whether its
        /// certificate is selected from RDP or supplied through <see cref="BindingCertificate"/>.
        /// The request fails when the selected protocol cannot meet this floor.
        /// </summary>
        /// <value>
        /// The minimum required <see cref="MtlsBindingStrength"/>. The default is
        /// <see cref="MtlsBindingStrength.None"/>, which imposes no floor and behaves identically
        /// to the parameterless mTLS PoP request. When set to a value greater than
        /// <see cref="MtlsBindingStrength.None"/>, the request fails with
        /// <see cref="MsalError.MinStrengthNotMet"/> if the host's maximum binding strength is
        /// lower than this value.
        /// </value>
        public MtlsBindingStrength MinStrength { get; set; } = MtlsBindingStrength.None;
    }
}
