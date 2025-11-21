// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal static class PersistentCertificateCacheFactory
    {
        private const string DisableEnvVar = "MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE";

        public static IPersistentCertificateCache Create(ILoggerAdapter logger)
        {
            var disable = Environment.GetEnvironmentVariable(DisableEnvVar);
            if (!string.IsNullOrEmpty(disable) &&
                (disable.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                 disable.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                logger.Info(() => "[PersistentCert] No-op persistent cache enabled via " + DisableEnvVar + ".");
                return new NoOpPersistentCertificateCache();
            }

            // We persist only on Windows because FriendlyName tagging is required.
            return DesktopOsHelper.IsWindows()
                ? new WindowsPersistentCertificateCache()
                : new NoOpPersistentCertificateCache();
        }
    }
}
