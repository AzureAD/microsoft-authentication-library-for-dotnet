// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        /// Gets or sets the minimum binding strength the host must be able to produce for the
        /// request to succeed. This is a <b>floor assertion</b>, not a downgrade selector: MSAL
        /// always uses the host's maximum binding strength, and the request fails when the host
        /// cannot meet this floor.
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
