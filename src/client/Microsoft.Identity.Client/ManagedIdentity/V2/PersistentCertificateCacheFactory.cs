// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal static class PersistentCertificateCacheFactory
    {
        public static IPersistentCertificateCache Create(ILoggerAdapter logger)
        {
            if (DesktopOsHelper.IsWindows())
            {
                logger?.Info(() =>
                    "[PersistentCert] Windows detected; enabling persistent mTLS binding certificate cache (CurrentUser/My).");
                return new WindowsPersistentCertificateCache();
            }

            logger?.Info(() =>
                "[PersistentCert] Persistent mTLS binding certificate cache disabled on this platform; using in-memory cache only.");
            return new NoOpPersistentCertificateCache();
        }
    }
}
