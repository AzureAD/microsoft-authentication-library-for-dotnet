// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// A single canonical "known cloud" record: the authority host aliases,
    /// preferred network/cache hosts, and the (bare) FIC token exchange audience.
    /// </summary>
    /// <remarks>
    /// This is the single internal source of truth for MSAL's built-in cloud data.
    /// Both the public <see cref="KnownCloudMetadata"/> and the internal
    /// <see cref="KnownMetadataProvider"/> project their views from <see cref="KnownCloudData.Entries"/>,
    /// so the alias/preferred-host magic strings live in exactly one place.
    /// </remarks>
    internal sealed class KnownCloudEntry
    {
        public string[] Aliases { get; init; }

        public string PreferredNetwork { get; init; }

        public string PreferredCache { get; init; }

        /// <summary>
        /// The FIC token exchange audience URI, stored WITHOUT the <c>/.default</c> suffix.
        /// <c>null</c> for clouds without a known token exchange application.
        /// </summary>
        public string FederatedCredentialAudience { get; init; }

        /// <summary>
        /// <c>true</c> only for the public (commercial) cloud; used by
        /// <see cref="KnownMetadataProvider.IsPublicEnvironment(string)"/>.
        /// </summary>
        public bool IsPublic { get; init; }
    }

    /// <summary>
    /// The single internal source of truth for MSAL's built-in, publicly known Azure clouds.
    /// </summary>
    internal static class KnownCloudData
    {
        // Adding a cloud or a cloud-specific value here updates BOTH the public KnownCloudMetadata
        // and the internal KnownMetadataProvider.
        public static readonly IReadOnlyList<KnownCloudEntry> Entries = new[]
        {
            new KnownCloudEntry
            {
                Aliases = new[] { "login.microsoftonline.com", "login.windows.net", "login.microsoft.com", "sts.windows.net" },
                PreferredNetwork = "login.microsoftonline.com",
                PreferredCache = "login.windows.net",
                FederatedCredentialAudience = "api://AzureADTokenExchange",
                IsPublic = true,
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.partner.microsoftonline.cn", "login.chinacloudapi.cn" },
                PreferredNetwork = "login.partner.microsoftonline.cn",
                PreferredCache = "login.partner.microsoftonline.cn",
                FederatedCredentialAudience = "api://AzureADTokenExchangeChina",
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.microsoftonline.de" },
                PreferredNetwork = "login.microsoftonline.de",
                PreferredCache = "login.microsoftonline.de",
                FederatedCredentialAudience = null, // Microsoft Cloud Germany (decommissioned 2021); no token-exchange application exists.
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.microsoftonline.us", "login.usgovcloudapi.net" },
                PreferredNetwork = "login.microsoftonline.us",
                PreferredCache = "login.microsoftonline.us",
                FederatedCredentialAudience = "api://AzureADTokenExchangeUSGov",
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login-us.microsoftonline.com" },
                PreferredNetwork = "login-us.microsoftonline.com",
                PreferredCache = "login-us.microsoftonline.com",
                FederatedCredentialAudience = "api://AzureADTokenExchange", // Public-cloud alternate host; uses the public token-exchange audience.
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.windows-ppe.net", "sts.windows-ppe.net", "login.microsoft-ppe.com" },
                PreferredNetwork = "login.windows-ppe.net",
                PreferredCache = "login.windows-ppe.net",
                FederatedCredentialAudience = "api://AzureADTokenExchangePpe",
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.sovcloud-identity.fr" },
                PreferredNetwork = "login.sovcloud-identity.fr",
                PreferredCache = "login.sovcloud-identity.fr",
                FederatedCredentialAudience = "api://AzureADTokenExchangeFrance",
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.sovcloud-identity.de" },
                PreferredNetwork = "login.sovcloud-identity.de",
                PreferredCache = "login.sovcloud-identity.de",
                FederatedCredentialAudience = "api://AzureADTokenExchangeGermany",
            },
            new KnownCloudEntry
            {
                Aliases = new[] { "login.sovcloud-identity.sg" },
                PreferredNetwork = "login.sovcloud-identity.sg",
                PreferredCache = "login.sovcloud-identity.sg",
                FederatedCredentialAudience = "api://AzureADTokenExchangeGovSG", // Singapore sovereign cloud is registered under its "GovSG" codename, not "Singapore".
            },
        };
    }
}
