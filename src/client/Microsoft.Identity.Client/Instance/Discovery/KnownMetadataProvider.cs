// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class KnownMetadataProvider : IKnownMetadataProvider
    {
        // No need to use a ConcurrentDictionary, because the normal Dictionary is thread safe for read operations
        private static readonly IDictionary<string, InstanceDiscoveryMetadataEntry> s_knownEntries =
            new Dictionary<string, InstanceDiscoveryMetadataEntry>();

        private static readonly ISet<string> s_knownEnvironments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ISet<string> s_knownPublicEnvironments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static KnownMetadataProvider()
        {
            void AddToKnownCache(InstanceDiscoveryMetadataEntry entry)
            {
                foreach (string alias in entry.Aliases)
                {
                    s_knownEntries[alias] = entry;
                    s_knownEnvironments.Add(alias);
                }
            }

            void AddToPublicEnvironment(InstanceDiscoveryMetadataEntry entry)
            {
                foreach (string alias in entry.Aliases)
                {
                    s_knownPublicEnvironments.Add(alias);
                }
            }

            InstanceDiscoveryMetadataEntry publicCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.microsoftonline.com", "login.windows.net", "login.microsoft.com", "sts.windows.net" },
                PreferredNetwork = "login.microsoftonline.com",
                PreferredCache = "login.windows.net"
            };

            InstanceDiscoveryMetadataEntry cloudEntryChina = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.partner.microsoftonline.cn", "login.chinacloudapi.cn" },
                PreferredNetwork = "login.partner.microsoftonline.cn",
                PreferredCache = "login.partner.microsoftonline.cn"
            };

            InstanceDiscoveryMetadataEntry cloudEntryLegacyGermany = new InstanceDiscoveryMetadataEntry()
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

            InstanceDiscoveryMetadataEntry ppeCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.windows-ppe.net", "sts.windows-ppe.net", "login.microsoft-ppe.com" },
                PreferredNetwork = "login.windows-ppe.net",
                PreferredCache = "login.windows-ppe.net"
            };

            InstanceDiscoveryMetadataEntry bleuCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.sovcloud-identity.fr" },
                PreferredNetwork = "login.sovcloud-identity.fr",
                PreferredCache = "login.sovcloud-identity.fr"
            };

            InstanceDiscoveryMetadataEntry delosCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.sovcloud-identity.de" },
                PreferredNetwork = "login.sovcloud-identity.de",
                PreferredCache = "login.sovcloud-identity.de"
            };

            InstanceDiscoveryMetadataEntry govSGCloudEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "login.sovcloud-identity.sg" },
                PreferredNetwork = "login.sovcloud-identity.sg",
                PreferredCache = "login.sovcloud-identity.sg"
            };

            AddToKnownCache(publicCloudEntry);
            AddToKnownCache(cloudEntryChina);
            AddToKnownCache(cloudEntryLegacyGermany);
            AddToKnownCache(usGovCloudEntry);
            AddToKnownCache(usCloudEntry);
            AddToKnownCache(ppeCloudEntry);
            AddToKnownCache(bleuCloudEntry);
            AddToKnownCache(delosCloudEntry);
            AddToKnownCache(govSGCloudEntry);
            AddToPublicEnvironment(publicCloudEntry);
        }

        public static bool IsPublicEnvironment(string environment)
        {
            return s_knownPublicEnvironments.Contains(environment);
        }

        public InstanceDiscoveryMetadataEntry GetMetadata(
            string environment,
            IEnumerable<string> existingEnvironmentsInCache,
            ILoggerAdapter logger)
        {
            if (existingEnvironmentsInCache == null)
            {
                existingEnvironmentsInCache = Enumerable.Empty<string>();
            }

            bool canUseProvider = existingEnvironmentsInCache.All(e => s_knownEnvironments.ContainsOrdinalIgnoreCase(e));

            if (canUseProvider)
            {
                s_knownEntries.TryGetValue(environment, out InstanceDiscoveryMetadataEntry entry);
                logger.Verbose(() => $"[Instance Discovery] Tried to use known metadata provider for {environment}. Success? {entry != null}. ");

                return entry;
            }

            logger.VerbosePii(
                () => $"[Instance Discovery] Could not use known metadata provider because at least one environment in the cache is not known. Environments in cache: {string.Join(" ", existingEnvironmentsInCache)} ",
                () => $"[Instance Discovery] Could not use known metadata provider because at least one environment in the cache is not known. ");
            return null;
        }

        public static bool IsKnownEnvironment(string environment)
        {
            return s_knownEnvironments.Contains(environment);
        }

        public static bool TryGetKnownEnviromentPreferredNetwork(string environment, out string preferredNetworkEnvironment)
        {
            if (s_knownEntries.TryGetValue(environment, out var entry))
            {
                preferredNetworkEnvironment = entry.PreferredNetwork;
                return true;
            }

            preferredNetworkEnvironment = null;
            return false;
        }

        public static IDictionary<string, InstanceDiscoveryMetadataEntry> GetAllEntriesForTest()
        {
            return s_knownEntries;
        }
    }
}
