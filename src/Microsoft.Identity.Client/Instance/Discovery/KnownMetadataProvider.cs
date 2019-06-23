// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class KnownMetadataProvider : IKnownMetadataProvider
    {
        // No need to use a ConcurrentDictionary, because the normal Dictionary is thread safe for read operations
        private static readonly IDictionary<string, InstanceDiscoveryMetadataEntry> s_knownEntries =
            new Dictionary<string, InstanceDiscoveryMetadataEntry>();

        private static readonly ISet<string> s_knownEnvironments = new HashSet<string>();

        static KnownMetadataProvider()
        {
            InstanceDiscoveryMetadataEntry publicCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.microsoftonline.com", "login.windows.net", "login.microsoft.com", "sts.windows.net" },
                PreferredNetwork = "login.microsoftonline.com",
                PreferredCache = "sts.windows.net"
            };

            InstanceDiscoveryMetadataEntry cnCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.partner.microsoftonline.cn", "login.chinacloudapi.cn" },
                PreferredNetwork = "login.partner.microsoftonline.cn",
                PreferredCache = "login.partner.microsoftonline.cn"
            };

            InstanceDiscoveryMetadataEntry deCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.microsoftonline.de" },
                PreferredNetwork = "login.microsoftonline.de",
                PreferredCache = "login.microsoftonline.de"
            };

            InstanceDiscoveryMetadataEntry usGovCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.microsoftonline.us", "login.usgovcloudapi.net" },
                PreferredNetwork = "login.microsoftonline.us",
                PreferredCache = "login.microsoftonline.us"
            };

            InstanceDiscoveryMetadataEntry usCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login-us.microsoftonline.com" },
                PreferredNetwork = "login-us.microsoftonline.com",
                PreferredCache = "login-us.microsoftonline.com"
            };

            AddToKnownCache(publicCloudEntry);
            AddToKnownCache(cnCloudEntry);
            AddToKnownCache(deCloudEntry);
            AddToKnownCache(usGovCloudEntry);
            AddToKnownCache(usCloudEntry);
        }

        private static void AddToKnownCache(InstanceDiscoveryMetadataEntry entry)
        {
            foreach (string alias in entry.Aliases)
            {
                s_knownEntries[alias] = entry;
                s_knownEnvironments.Add(alias);
            }
        }

        public InstanceDiscoveryMetadataEntry GetMetadata(string environment, IEnumerable<string> existingEnviromentsInCache)
        {
            if (existingEnviromentsInCache == null)
            {
                existingEnviromentsInCache = Enumerable.Empty<string>();
            }

            bool canUseProvider = existingEnviromentsInCache.All(e => s_knownEnvironments.ContainsOrdinalIgnoreCase(e));

            if (canUseProvider)
            {
                s_knownEntries.TryGetValue(environment, out InstanceDiscoveryMetadataEntry entry);
                return entry;
            }

            return null;
        }
    }
}
