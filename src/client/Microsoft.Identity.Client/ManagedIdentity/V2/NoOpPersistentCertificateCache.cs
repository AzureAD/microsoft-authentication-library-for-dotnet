// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Disabled persistence for platforms where FriendlyName tagging is unsupported.
    /// </summary>
    internal sealed class NoOpPersistentCertificateCache : IPersistentCertificateCache
    {
        public bool Read(string alias, out CertificateCacheValue value, ILoggerAdapter logger = null)
        {
            value = default;
            return false;
        }

        public void Write(string alias, X509Certificate2 cert, string endpointBase, ILoggerAdapter logger = null)
        {
            // no-op
        }

        public void Delete(string alias, ILoggerAdapter logger = null)
        {
            // no-op
        }
    }
}
