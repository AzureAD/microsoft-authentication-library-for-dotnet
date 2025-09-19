// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Internal helper for resolving a certificate from the local certificate stores (CurrentUser / LocalMachine).
    /// Looks first by subject name, then by friendly name. Returns newest (highest NotBefore) match.
    /// </summary>
    internal static class ManagedIdentityCertificateHelper
    {
        private const string LabAuthCertName = "LabAuth";

        internal static X509Certificate2 TryGetLabAuthCertificate(ILoggerAdapter logger)
        {
            return TryGetCertificate(LabAuthCertName, logger);
        }

        internal static X509Certificate2 TryGetCertificate(string name, ILoggerAdapter logger)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                try
                {
                    using var store = new X509Store(StoreName.My, location);
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                    // 1. Subject name search
                    var matches = store.Certificates.Find(X509FindType.FindBySubjectName, name, validOnly: false);

                    // 2. Fallback – friendly name or subject contains CN=name
                    if (matches.Count == 0)
                    {
                        var alt = new X509Certificate2Collection();
                        foreach (var c in store.Certificates)
                        {
                            if (string.Equals(c.FriendlyName, name, StringComparison.OrdinalIgnoreCase) ||
                                (c.SubjectName?.Name?.Contains($"CN={name}", StringComparison.OrdinalIgnoreCase) ?? false))
                            {
                                alt.Add(c);
                            }
                        }
                        matches = alt;
                    }

                    if (matches.Count > 0)
                    {
                        X509Certificate2 selected = matches[0];
                        for (int i = 1; i < matches.Count; i++)
                        {
                            if (matches[i].NotBefore > selected.NotBefore)
                            {
                                selected = matches[i];
                            }
                        }

                        logger?.Info($"[ManagedIdentityCertHelper] Found certificate '{name}' in {location}. Thumbprint: {selected.Thumbprint}");
                        return selected;
                    }
                }
                catch (Exception ex)
                {
                    logger?.Verbose(() => $"[ManagedIdentityCertHelper] Exception searching '{name}' in {location}: {ex.Message}");
                }
            }

            logger?.Info($"[ManagedIdentityCertHelper] Certificate '{name}' not found in CurrentUser or LocalMachine.");
            return null;
        }
    }
}
