// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Describes the managed identity capabilities detected for the current host, including the
    /// detected source and the strength with which tokens can be bound to a key.
    /// </summary>
    /// <remarks>
    /// This type is returned by <see cref="ManagedIdentityApplication.GetManagedIdentityCapabilitiesAsync"/>.
    /// It is useful for credential chains such as <c>DefaultAzureCredential</c> to decide whether
    /// managed identity is available and what binding strength the host supports.
    /// </remarks>
    public class ManagedIdentityCapabilities
    {
        /// <summary>
        /// Gets the detected managed identity source.
        /// </summary>
        /// <value>
        /// The <see cref="ManagedIdentitySource"/> detected on the environment, or
        /// <see cref="ManagedIdentitySource.None"/> if no source was detected. The internal
        /// IMDS v1/v2 distinction is not surfaced here; both report
        /// <see cref="ManagedIdentitySource.Imds"/>.
        /// </value>
        public ManagedIdentitySource Source { get; }

        /// <summary>
        /// Gets the reason detection failed or returned reduced capability, if any.
        /// </summary>
        /// <value>
        /// A single string describing why managed identity detection failed, or why the reported
        /// <see cref="MaxSupportedBindingStrength"/> is lower than the host could otherwise offer;
        /// <c>null</c> when neither applies.
        /// <para>
        /// A non-null value does <b>not</b> by itself mean managed identity is unavailable. When
        /// <see cref="Source"/> is anything other than <see cref="ManagedIdentitySource.None"/>, a
        /// source was detected and token acquisition can still succeed - for example when IMDSv2 has
        /// been turned off by the <c>MSAL_MI_DISABLE_IMDS_V2</c> environment variable, bearer tokens
        /// continue to work while binding is reported as
        /// <see cref="MtlsBindingStrength.None"/>. Branch on <see cref="Source"/> to decide whether
        /// managed identity is available, and on <see cref="MaxSupportedBindingStrength"/> to decide
        /// whether to request a bound token; treat this property as diagnostic text.
        /// </para>
        /// </value>
        public string ErrorReason { get; }

        /// <summary>
        /// Gets the highest binding strength the current host is capable of producing.
        /// </summary>
        /// <value>
        /// The strongest <see cref="MtlsBindingStrength"/> available on this host. This is the
        /// primary capability signal; callers should branch on it rather than on the source label.
        /// </value>
        public MtlsBindingStrength MaxSupportedBindingStrength { get; }

        /// <summary>
        /// Gets a value indicating whether the host can bind a token to a key (mTLS
        /// Proof-of-Possession).
        /// </summary>
        /// <value>
        /// <c>true</c> when <see cref="MaxSupportedBindingStrength"/> is greater than
        /// <see cref="MtlsBindingStrength.None"/>. This means the host can bind a token to a key;
        /// it does <b>not</b> imply hardware attestation. Callers that require attestation must
        /// check for the <see cref="MtlsBindingStrength.KeyGuard"/> tier.
        /// </value>
        public bool IsMtlsPopSupportedByHost => MaxSupportedBindingStrength > MtlsBindingStrength.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityCapabilities"/> class.
        /// </summary>
        /// <param name="source">The detected managed identity source.</param>
        /// <param name="maxSupportedBindingStrength">The highest binding strength the host supports.</param>
        /// <param name="errorReason">
        /// The reason detection failed, or the reason the reported binding strength is reduced;
        /// <c>null</c> when neither applies.
        /// </param>
        internal ManagedIdentityCapabilities(
            ManagedIdentitySource source,
            MtlsBindingStrength maxSupportedBindingStrength,
            string errorReason = null)
        {
            Source = source;
            MaxSupportedBindingStrength = maxSupportedBindingStrength;
            ErrorReason = errorReason;
        }
    }
}
