// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal static class PersistentCertificateCacheFactory
    {
        private const string EnableEnvVar = "MSAL_MI_ENABLE_PERSISTENT_CERT_CACHE";

        public static IPersistentCertificateCache Create(ILoggerAdapter logger)
        {
            string raw;
            try
            {
                raw = Environment.GetEnvironmentVariable(EnableEnvVar);
            }
            catch (System.Security.SecurityException)
            {
                // Persistence must never block authentication; if we cannot read the
                // environment we silently default to in-memory (NoOp) behavior.
                return new NoOpPersistentCertificateCache();
            }

            string value = raw?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return new NoOpPersistentCertificateCache();
            }

            bool optedIn =
                value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (!optedIn)
            {
                logger.Info(() => "[PersistentCert] " + EnableEnvVar + " is set to an unrecognized value (expected '1' or 'true'). Persistent cache will not be enabled.");
                return new NoOpPersistentCertificateCache();
            }

            if (DesktopOsHelper.IsWindows())
            {
                logger.Info(() => "[PersistentCert] Windows persistent cache enabled via " + EnableEnvVar + ".");
                return new WindowsPersistentCertificateCache();
            }

            logger.Info(() => "[PersistentCert] " + EnableEnvVar + " is set but persistent cache is only supported on Windows. Falling back to no-op.");
            return new NoOpPersistentCertificateCache();
        }
    }
}
