// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class UserMetadataProvider : IUserMetadataProvider
    {
        private readonly IDictionary<string, InstanceDiscoveryMetadataEntry> _entries =
             new Dictionary<string, InstanceDiscoveryMetadataEntry>();

        public UserMetadataProvider(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (InstanceDiscoveryMetadataEntry entry in instanceDiscoveryResponse?.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (string aliasedEnvironment in entry.Aliases ?? Enumerable.Empty<string>())
                {
                    _entries.Add(aliasedEnvironment, entry);
                }
            }
        }

        public InstanceDiscoveryMetadataEntry GetMetadataOrThrow(string environment, ICoreLogger logger)
        {
            _entries.TryGetValue(environment ?? "", out InstanceDiscoveryMetadataEntry entry);

            logger.Verbose($"[Instance Discovery] Tried to use user metadata provider for {environment}. Success? {entry != null}");

            if (entry == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidUserInstanceMetadata,
                    MsalErrorMessage.NoUserInstanceMetadataEntry(environment));
            }

            return entry;
        }

    }
}
