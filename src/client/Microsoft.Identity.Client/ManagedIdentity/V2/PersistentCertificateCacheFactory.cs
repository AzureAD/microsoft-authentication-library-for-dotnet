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
            // We persist only on Windows because FriendlyName tagging is required.
            return DesktopOsHelper.IsWindows()
                ? new WindowsPersistentCertificateCache()
                : new NoOpPersistentCertificateCache();
        }
    }
}
