// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    // Cloud entries here are derived from KnownCloudData (the single source of truth), which
    // KnownCloudConfiguration also projects from. Add or change clouds in KnownCloudData, not here.
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

            foreach (KnownCloudEntry cloud in KnownCloudData.Entries)
            {
                var entry = new InstanceDiscoveryMetadataEntry()
                {
                    Aliases = cloud.Aliases,
                    PreferredNetwork = cloud.PreferredNetwork,
                    PreferredCache = cloud.PreferredCache,
                };

                AddToKnownCache(entry);

                if (cloud.IsPublic)
                {
                    AddToPublicEnvironment(entry);
                }
            }
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
