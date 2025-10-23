// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache
{
    internal sealed partial class ImdsV2BindingCache : IBindingCache
    {
        public static ImdsV2BindingCache Shared { get; } = new ImdsV2BindingCache();

        private readonly ConcurrentDictionary<string, ImdsV2BindingMetadata> _map =
            new ConcurrentDictionary<string, ImdsV2BindingMetadata>();

        // ---- TEST HOOK ----
        public void ClearForTest() => _map.Clear();

        public void Cache(string identityKey, string tokenType,
                          CertificateRequestResponse resp, string subject)
        {
            var m = _map.GetOrAdd(identityKey, _ => new ImdsV2BindingMetadata());
            if (string.IsNullOrEmpty(m.Subject))
            {
                m.Subject = subject;
            }

            if (string.Equals(tokenType, Constants.BearerTokenType, System.StringComparison.OrdinalIgnoreCase))
            {
                m.BearerResponse = resp;
            }
            else
            {
                m.PopResponse = resp; // treat non-Bearer as mtls_pop
            }
        }

        public bool TryGet(string identityKey, string tokenType,
                           out CertificateRequestResponse resp, out string subject)
        {
            resp = null;
            subject = null;
            if (!_map.TryGetValue(identityKey, out var m))
                return false;

            subject = m.Subject;

            if (string.Equals(tokenType, Constants.BearerTokenType, System.StringComparison.OrdinalIgnoreCase))
            {
                resp = m.BearerResponse;
            }
            else
            {
                resp = m.PopResponse;
            }

            return resp != null && !string.IsNullOrEmpty(subject);
        }

        public bool TryGetAnyPop(out CertificateRequestResponse resp, out string subject)
        {
            resp = null;
            subject = null;

            foreach (var kv in _map)
            {
                var m = kv.Value;
                if (m?.PopResponse != null && !string.IsNullOrEmpty(m.Subject))
                {
                    resp = m.PopResponse;
                    subject = m.Subject;
                    return true;
                }
            }

            return false;
        }
    }
}
