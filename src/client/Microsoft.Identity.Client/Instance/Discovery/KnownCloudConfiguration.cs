// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Default <see cref="ICloudConfiguration"/> implementation that provides
    /// cloud-specific metadata for all publicly known Azure cloud environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class reuses the authority host alias data from MSAL's internal
    /// <c>KnownMetadataProvider</c> and adds <see cref="CloudSettings.TokenExchangeAudience"/>
    /// for clouds that have a known FIC token exchange application.
    /// </para>
    /// <para>
    /// Higher-level SDKs (e.g., MISE) can implement <see cref="ICloudConfiguration"/>
    /// to extend this with entries for internal-only clouds. Customers can provide a
    /// custom implementation via the application builder to add private cloud entries.
    /// </para>
    /// </remarks>
    public class KnownCloudConfiguration : ICloudConfiguration
    {
        /// <summary>
        /// Singleton instance of the default cloud configuration.
        /// </summary>
        public static KnownCloudConfiguration Default { get; } = new KnownCloudConfiguration();

        private static readonly Dictionary<string, CloudSettings> s_cloudSettingsByAlias =
            new Dictionary<string, CloudSettings>(StringComparer.OrdinalIgnoreCase);

        static KnownCloudConfiguration()
        {
            void Register(CloudSettings entry)
            {
                if (entry.Aliases is not null)
                {
                    foreach (string alias in entry.Aliases)
                    {
                        s_cloudSettingsByAlias[alias] = entry;
                    }
                }
            }

            Register(new CloudSettings
            {
                Aliases = new[] { "login.microsoftonline.com", "login.windows.net", "login.microsoft.com", "sts.windows.net" },
                PreferredNetwork = "login.microsoftonline.com",
                PreferredCache = "login.windows.net",
                TokenExchangeAudience = "api://AzureADTokenExchange",
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.partner.microsoftonline.cn", "login.chinacloudapi.cn" },
                PreferredNetwork = "login.partner.microsoftonline.cn",
                PreferredCache = "login.partner.microsoftonline.cn",
                TokenExchangeAudience = "api://AzureADTokenExchangeChina",
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.microsoftonline.de" },
                PreferredNetwork = "login.microsoftonline.de",
                PreferredCache = "login.microsoftonline.de",
                TokenExchangeAudience = null, // Deprecated cloud
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.microsoftonline.us", "login.usgovcloudapi.net" },
                PreferredNetwork = "login.microsoftonline.us",
                PreferredCache = "login.microsoftonline.us",
                TokenExchangeAudience = "api://AzureADTokenExchangeUSGov",
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login-us.microsoftonline.com" },
                PreferredNetwork = "login-us.microsoftonline.com",
                PreferredCache = "login-us.microsoftonline.com",
                TokenExchangeAudience = null,
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.windows-ppe.net", "sts.windows-ppe.net", "login.microsoft-ppe.com" },
                PreferredNetwork = "login.windows-ppe.net",
                PreferredCache = "login.windows-ppe.net",
                TokenExchangeAudience = null,
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.sovcloud-identity.fr" },
                PreferredNetwork = "login.sovcloud-identity.fr",
                PreferredCache = "login.sovcloud-identity.fr",
                TokenExchangeAudience = "api://AzureADTokenExchangeFrance",
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.sovcloud-identity.de" },
                PreferredNetwork = "login.sovcloud-identity.de",
                PreferredCache = "login.sovcloud-identity.de",
                TokenExchangeAudience = "api://AzureADTokenExchangeGermany",
            });

            Register(new CloudSettings
            {
                Aliases = new[] { "login.sovcloud-identity.sg" },
                PreferredNetwork = "login.sovcloud-identity.sg",
                PreferredCache = "login.sovcloud-identity.sg",
                TokenExchangeAudience = null,
            });
        }

        /// <inheritdoc/>
        public CloudSettings GetSettingsByAuthority(string authorityHost)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                return null;
            }

            s_cloudSettingsByAlias.TryGetValue(authorityHost, out CloudSettings settings);
            return settings;
        }
    }
}
