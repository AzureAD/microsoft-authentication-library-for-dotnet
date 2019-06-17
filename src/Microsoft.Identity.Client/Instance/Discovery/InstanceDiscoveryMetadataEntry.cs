// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    [DataContract]
    internal sealed class InstanceDiscoveryMetadataEntry
    {
        [DataMember(Name = "preferred_network")]
        public string PreferredNetwork { get; set; }

        [DataMember(Name = "preferred_cache")]
        public string PreferredCache { get; set; }

        [DataMember(Name = "aliases")]
        public string[] Aliases { get; set; }

        public IEnumerable<string> GetAliasesWithPreferredCacheFirst()
        {
            var list = new List<string>(new[] { PreferredCache });
            foreach (var alias in Aliases)
            {
                if (!string.Equals(PreferredCache, alias, System.StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(alias);
                }
            }

            return list;
        }
    }
}
