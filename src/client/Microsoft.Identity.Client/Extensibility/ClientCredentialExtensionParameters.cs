// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Provides application configuration context to client credential extensibility callbacks.
    /// Contains read-only information about the confidential client application.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public class ClientCredentialExtensionParameters
    {
        /// <summary>
        /// Internal constructor - only MSAL can create instances of this class.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        internal ClientCredentialExtensionParameters(ApplicationConfiguration config)
        {
            ClientId = config.ClientId;
            TenantId = config.TenantId;
            Authority = config.Authority?.AuthorityInfo?.CanonicalAuthority?.ToString();
        }

        /// <summary>
        /// The application (client) ID as registered in the Azure portal or application registration portal.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// The tenant ID if the application is configured for a specific tenant.
        /// Will be null for multi-tenant applications.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// The authority URL used for authentication (e.g., https://login.microsoftonline.com/common).
        /// </summary>
        public string Authority { get; }
    }
}
