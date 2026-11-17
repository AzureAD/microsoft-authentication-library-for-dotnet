// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Well-known keys for the cloud-specific metadata returned by
    /// <see cref="KnownCloudMetadata.GetByAuthorityHost(string)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cloud metadata is returned as a string-keyed dictionary rather than as fixed typed properties, so
    /// that new cloud-specific values can be added — and obsolete ones removed — without source- or
    /// binary-breaking callers. Callers read values by these key constants.
    /// </para>
    /// <para>
    /// These are MSAL's <b>public</b> keys. Higher-level SDKs that carry additional, internal-only cloud
    /// values define their own key namespace and translate to these keys before handing a value to MSAL.
    /// </para>
    /// <para>
    /// Preferred network/cache hosts are deliberately <b>not</b> exposed here: MSAL's instance-discovery
    /// pipeline resolves them from its own internal table and never reads them from this dictionary, so a
    /// value placed here would be inert. To override preferred hosts, use
    /// <see cref="AbstractApplicationBuilder{T}.WithInstanceDiscoveryMetadata(System.Uri)"/> (or the JSON
    /// overload) instead.
    /// </para>
    /// </remarks>
    public static class CloudMetadataKeyNames
    {
        /// <summary>
        /// The cloud-specific FIC (Federated Identity Credential) token-exchange audience URI, stored
        /// <b>without</b> the <c>/.default</c> suffix (e.g., "api://AzureADTokenExchange"). Use
        /// <see cref="TokenExchangeScope.FromAudience(string)"/> to obtain the scope form (with
        /// <c>/.default</c>) for client-credentials flows.
        /// </summary>
        public const string FederatedCredentialAudience = "federated_credential_audience";
    }
}
