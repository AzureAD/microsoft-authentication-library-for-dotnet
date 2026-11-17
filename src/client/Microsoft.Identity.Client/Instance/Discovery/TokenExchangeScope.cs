// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Computes the FIC (Federated Identity Credential) token-exchange <b>scope</b> from a bare audience.
    /// </summary>
    /// <remarks>
    /// Cloud-specific audiences are stored <b>bare</b> (without <c>/.default</c>); the scope form is
    /// computed here so no call site hand-appends the suffix. This type is the single owner of the
    /// audience→scope rule.
    /// </remarks>
    public static class TokenExchangeScope
    {
        private const string DefaultSuffix = "/.default";

        /// <summary>
        /// Returns the FIC token-exchange <b>scope</b> for a bare audience — the audience with
        /// <c>/.default</c> appended — suitable for client-credentials / app-token contexts.
        /// </summary>
        /// <param name="audience">
        /// The bare token-exchange audience URI (e.g., "api://AzureADTokenExchange"), typically read from
        /// <see cref="KnownCloudMetadata.GetByAuthorityHost(string)"/> under
        /// <see cref="CloudMetadataKeyNames.FederatedCredentialAudience"/>.
        /// </param>
        /// <returns>
        /// The audience with <c>/.default</c> appended; the input unchanged if it already ends with
        /// <c>/.default</c>; or <c>null</c> if <paramref name="audience"/> is null or empty.
        /// </returns>
        public static string FromAudience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                return null;
            }

            return audience.EndsWith(DefaultSuffix, StringComparison.OrdinalIgnoreCase)
                ? audience
                : audience + DefaultSuffix;
        }
    }
}
