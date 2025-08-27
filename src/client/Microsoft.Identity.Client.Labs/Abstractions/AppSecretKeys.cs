// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Declares the Key Vault secret names for an application's credentials.
    /// Each property is a secret <em>name</em> (not the secret value).
    /// </summary>
    public sealed class AppSecretKeys
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppSecretKeys"/> class.
        /// For optional secrets, pass an empty string (<c>""</c>) to indicate "not configured".
        /// </summary>
        /// <param name="clientIdSecret">Key Vault secret name that stores the application's client ID.</param>
        /// <param name="clientSecretSecret">Optional Key Vault secret name that stores the application's client secret. Use <c>""</c> if not used.</param>
        /// <param name="pfxSecret">Optional Key Vault secret name that stores a Base64-encoded PFX certificate. Use <c>""</c> if not used.</param>
        /// <param name="pfxPasswordSecret">Optional Key Vault secret name that stores the password for the PFX. Use <c>""</c> if not used.</param>
        public AppSecretKeys(
            string clientIdSecret,
            string clientSecretSecret = "",
            string pfxSecret = "",
            string pfxPasswordSecret = "")
        {
            ClientIdSecret = clientIdSecret;
            ClientSecretSecret = clientSecretSecret ?? string.Empty;
            PfxSecret = pfxSecret ?? string.Empty;
            PfxPasswordSecret = pfxPasswordSecret ?? string.Empty;
        }

        /// <summary>
        /// Gets the Key Vault secret name that stores the application's client ID.
        /// </summary>
        public string ClientIdSecret { get; }

        /// <summary>
        /// Gets the Key Vault secret name that stores the application's client secret.
        /// Empty string indicates "not configured".
        /// </summary>
        public string ClientSecretSecret { get; }

        /// <summary>
        /// Gets the Key Vault secret name that stores a Base64-encoded PFX certificate.
        /// Empty string indicates "not configured".
        /// </summary>
        public string PfxSecret { get; }

        /// <summary>
        /// Gets the Key Vault secret name that stores the password for the PFX certificate.
        /// Empty string indicates "not configured".
        /// </summary>
        public string PfxPasswordSecret { get; }
    }
}
