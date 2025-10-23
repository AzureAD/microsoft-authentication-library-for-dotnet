// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions
{
    internal interface IBindingCache
    {
        void Cache(string identityKey, string tokenType,
                    CertificateRequestResponse resp, string subject);

        bool TryGet(string identityKey, string tokenType,
                    out CertificateRequestResponse resp, out string subject);

        // Test hook: allow “any identity” reuse for PoP if needed
        bool TryGetAnyPop(out CertificateRequestResponse resp, out string subject);
    }
}
