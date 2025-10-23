// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache
{
    internal sealed class MtlsCertCacheEntry
    {
        public X509Certificate2 Certificate { get; }
        public object IssueCredentialResponse { get; }
        public string KeyHandle { get; }
        public DateTimeOffset NotBefore { get; }
        public DateTimeOffset NotAfter { get; }
        public DateTimeOffset CreatedAtUtc { get; }

        public MtlsCertCacheEntry(X509Certificate2 certificate, object issueCredentialResponse,
                                  string keyHandle, DateTimeOffset createdAtUtc)
        {
            Certificate = certificate;
            IssueCredentialResponse = issueCredentialResponse;
            KeyHandle = keyHandle;
            CreatedAtUtc = createdAtUtc;
            NotBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime());
            NotAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime());
        }
    }
}
