// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Cloud-specific metadata for a single Azure cloud environment.
    /// Contains transport-layer settings (preferred hosts, aliases) and
    /// application-layer settings (token exchange audiences) for a cloud.
    /// </summary>
    /// <remarks>
    /// This class is extensible — additional cloud-specific properties can be
    /// added as new cross-cloud scenarios are identified without breaking changes.
    /// </remarks>
    public class CloudSettings
    {
        /// <summary>
        /// The preferred host to use for network requests to this cloud
        /// (e.g., "login.microsoftonline.com").
        /// </summary>
        public string PreferredNetwork { get; set; }

        /// <summary>
        /// The preferred host to use as the cache key for this cloud
        /// (e.g., "login.windows.net").
        /// </summary>
        public string PreferredCache { get; set; }

        /// <summary>
        /// All known host aliases for this cloud. Tokens issued by any alias
        /// are equivalent and share a cache entry.
        /// </summary>
        public string[] Aliases { get; set; }

        /// <summary>
        /// The cloud-specific FIC (Federated Identity Credential) token exchange
        /// audience URI, without the <c>/.default</c> suffix.
        /// For example, <c>"api://AzureADTokenExchange"</c> for the public cloud,
        /// <c>"api://AzureADTokenExchangeUSGov"</c> for US Government.
        /// </summary>
        /// <remarks>
        /// <c>null</c> for clouds that do not have a known token exchange application.
        /// Callers should append <c>/.default</c> when using this value as a scope
        /// in the client credentials flow, and omit it for the managed identity flow.
        /// </remarks>
        public string TokenExchangeAudience { get; set; }
    }
}
