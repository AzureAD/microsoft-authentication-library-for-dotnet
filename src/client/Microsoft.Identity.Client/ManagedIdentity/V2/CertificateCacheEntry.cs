// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal sealed class CertificateCacheEntry : IDisposable
    {
        internal static readonly TimeSpan MinRemainingLifetime = TimeSpan.FromHours(24);

        public CertificateCacheEntry(X509Certificate2 certificate, DateTimeOffset notAfterUtc, string endpoint, string clientId)
        {
            Certificate = certificate ?? throw new MsalClientException(nameof(certificate));
            NotAfterUtc = notAfterUtc;
            Endpoint = endpoint ?? throw new MsalClientException(nameof(endpoint));
            ClientId = clientId ?? throw new MsalClientException(nameof(clientId));
        }

        public X509Certificate2 Certificate { get; }
        public DateTimeOffset NotAfterUtc { get; }
        public string Endpoint { get; }
        public string ClientId { get; }

        public bool IsExpiredUtc(DateTimeOffset nowUtc)
        {
            return nowUtc >= (NotAfterUtc - MinRemainingLifetime);
        }

        public void Dispose() { Certificate.Dispose(); }
    }
}
