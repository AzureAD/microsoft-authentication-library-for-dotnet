// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        // Shape check for the {region} label in a regional host, e.g. "westus2" in
        // "westus2.login.microsoft.com". DNS-label rules per RFC 1035/1123:
        // lowercase alphanumeric + optional internal hyphens, max 63 chars.
        // Uri.Host is already lowercased, so IgnoreCase isn't needed. The real allow-list
        // is the base-host lookup in TryResolveKnownCloud.
        private static readonly Regex s_regionPrefixRegex = new Regex(
            "^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        /// <summary>
        /// True only when both hosts resolve to the same Microsoft sovereign cloud.
        /// e.g. (login.microsoftonline.com, sts.windows.net) → true; (Public, China) → false;
        /// either host unknown → false. Regional variants share their base host's cloud.
        /// </summary>
        internal static bool AreInSameCloud(string hostA, string hostB)
        {
            if (!TryResolveKnownCloud(hostA, out InstanceDiscoveryMetadataEntry entryA))
            {
                return false;
            }

            if (!TryResolveKnownCloud(hostB, out InstanceDiscoveryMetadataEntry entryB))
            {
                return false;
            }

            return ReferenceEquals(entryA, entryB);
        }

        // Exact lookup, else strip a single well-formed regional prefix ("westus2.") and
        // re-lookup the base host. Anything else (multi-label, bad shape) → false.
        // Returns the singleton entry per cloud (see ctor); callers may compare via ReferenceEquals.
        internal static bool TryResolveKnownCloud(string host, out InstanceDiscoveryMetadataEntry entry)
        {
            if (string.IsNullOrEmpty(host))
            {
                entry = null;
                return false;
            }

            if (s_knownEntries.TryGetValue(host, out entry))
            {
                return true;
            }

            int firstDot = host.IndexOf('.');
            if (firstDot > 0 && firstDot < host.Length - 1)
            {
                string regionPrefix = host.Substring(0, firstDot);
                if (s_regionPrefixRegex.IsMatch(regionPrefix))
                {
                    string baseHost = host.Substring(firstDot + 1);
                    if (s_knownEntries.TryGetValue(baseHost, out entry))
                    {
                        return true;
                    }
                }
            }

            entry = null;
            return false;
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
