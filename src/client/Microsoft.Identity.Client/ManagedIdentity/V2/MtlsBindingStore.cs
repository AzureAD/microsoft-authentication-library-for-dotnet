// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Manages X.509 certificates for Mutual TLS (MTLS) authentication in the user's certificate store.
    /// This class provides persistent storage and lifecycle management for certificates used in
    /// managed identity authentication scenarios.
    /// </summary>
    /// <remarks>
    /// MtlsBindingStore handles several aspects of certificate management:
    /// 
    /// 1. Certificate installation and retrieval:
    ///    - Installing certificates to CurrentUser\My store
    ///    - Retrieving certificates by thumbprint or subject
    ///    - Finding the freshest (most long-lived) certificate
    /// 
    /// 2. Certificate lifecycle management:
    ///    - Validating certificate freshness and expiration
    ///    - Detecting half-life for rotation decisions
    ///    - Purging expired certificates beyond retention window
    /// 
    /// 3. Certificate rotation strategies:
    ///    - Foreground rotation: Remove older certificates, keep only specified thumbprint
    ///    - Background rotation: Only purge very stale certificates, preserve valid ones
    /// 
    /// All store operations are best-effort with appropriate error handling to accommodate
    /// potential access restrictions in different environments.
    /// </remarks>
    internal static class MtlsBindingStore
    {
        internal static readonly TimeSpan ExpiredPurgeWindow = TimeSpan.FromDays(7);

        internal static bool IsCurrentlyValid(X509Certificate2 cert)
        {
            if (cert == null)
                return false;
            
            bool isValid = DateTime.UtcNow < cert.NotAfter.ToUniversalTime();
            return isValid;
        }

        internal static bool IsBeyondHalfLife(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
                return false;

            var nb = cert.NotBefore.ToUniversalTime();
            var na = cert.NotAfter.ToUniversalTime();

            if (na <= nb)
            {
                logger?.Warning($"[Managed Identity] Certificate has invalid validity period: NotBefore={nb}, NotAfter={na}");
                return true; // defensive
            }

            var halfLife = nb + TimeSpan.FromTicks((na - nb).Ticks / 2);
            var isBeyond = DateTime.UtcNow >= halfLife;
            
            if (isBeyond && logger?.IsLoggingEnabled(LogLevel.Info) == true)
            {
                var now = DateTime.UtcNow;
                var timeUntilExpiry = na - now;
                logger.Info(() => $"[Managed Identity] Certificate {cert.Thumbprint} is beyond half-life. " +
                                  $"Valid: {nb:u} to {na:u}, Half-life: {halfLife:u}, Now: {now:u}, Time remaining: {timeUntilExpiry.TotalHours:F1} hours");
            }
            
            return isBeyond;
        }

        internal static string InstallAndGetSubject(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
            {
                logger?.Warning("[Managed Identity] Cannot install null certificate");
                return null;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                logger?.Verbose(() => $"[Managed Identity] Installing certificate with thumbprint {cert.Thumbprint}, subject: {cert.Subject}, valid until: {cert.NotAfter:u}");

                var dupes = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
                if (dupes.Count > 0)
                {
                    logger?.Info(() => $"[Managed Identity] Removing {dupes.Count} duplicate certificate(s) with thumbprint {cert.Thumbprint}");
                    foreach (var d in dupes)
                    { 
                        try { store.Remove(d); }
                        catch (Exception ex) 
                        { 
                            logger?.Warning($"[Managed Identity] Failed to remove duplicate certificate: {ex.Message}");
                        }
                    }
                }

                store.Add(cert);
                logger?.Info($"[Managed Identity] Successfully installed certificate with thumbprint {cert.Thumbprint}");
                return cert.Subject;
            }
            catch (Exception ex)
            {
                logger?.Warning($"[Managed Identity] Failed to install binding cert: {ex.Message}");
                return cert.Subject;
            }
        }

        internal static X509Certificate2 GetFreshestBySubject(string subject, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot find certificates with null or empty subject");
                return null;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                logger?.Verbose(() => $"[Managed Identity] Searching for certificates with subject: {subject}");

                var certs = store.Certificates
                    .Find(X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false)
                    .OfType<X509Certificate2>()
                    .ToList();
                
                if (certs.Count == 0)
                {
                    logger?.Info(() => $"[Managed Identity] No certificates found with subject: {subject}");
                    return null;
                }
                
                logger?.Verbose(() => $"[Managed Identity] Found {certs.Count} certificates with subject: {subject}");
                
                var freshest = certs.OrderByDescending(c => c.NotAfter.ToUniversalTime()).First();
                
                logger?.Info(() => $"[Managed Identity] Selected freshest certificate with thumbprint: {freshest.Thumbprint}, valid until: {freshest.NotAfter:u}");
                return freshest;
            }
            catch (Exception ex)
            {
                logger?.Warning($"[Managed Identity] Failed to read binding cert from user store: {ex.Message}");
                return null;
            }
        }

        internal static void PruneOlder(string subject, string keepThumbprint, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(keepThumbprint))
            {
                logger?.Warning($"[Managed Identity] Cannot prune with null/empty subject or thumbprint. Subject: '{subject}', Thumbprint: '{keepThumbprint}'");
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var now = DateTime.UtcNow;
                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Pruning certificates: found {matches.Count} with subject '{subject}', keeping thumbprint '{keepThumbprint}'");
                
                int removedCount = 0;
                int expiredCount = 0;

                foreach (var c in matches.OfType<X509Certificate2>())
                {
                    var isKeep = string.Equals(c.Thumbprint, keepThumbprint, StringComparison.OrdinalIgnoreCase);
                    var expiredBeyondWindow = c.NotAfter.ToUniversalTime() < (now - ExpiredPurgeWindow);

                    if (!isKeep || expiredBeyondWindow)
                    {
                        try
                        { 
                            if (expiredBeyondWindow)
                            {
                                logger?.Verbose(() => $"[Managed Identity] Removing certificate {c.Thumbprint} expired beyond window (expired {(now - c.NotAfter.ToUniversalTime()).TotalDays:F1} days ago)");
                                expiredCount++;
                            }
                            else
                            {
                                logger?.Verbose(() => $"[Managed Identity] Removing older certificate {c.Thumbprint} (valid until {c.NotAfter:u})");
                                removedCount++;
                            }
                            
                            store.Remove(c); 
                        }
                        catch (Exception ex) 
                        { 
                            logger?.Warning($"[Managed Identity] Failed to remove certificate {c.Thumbprint}: {ex.Message}");
                        }
                    }
                }

                logger?.Info($"[Managed Identity] Pruning complete: removed {removedCount} older certificates and {expiredCount} expired certificates");
            }
            catch (Exception ex)
            { 
                logger?.Warning($"[Managed Identity] Certificate pruning failed: {ex.Message}");
            }
        }

        internal static void RemoveAllBySubject(string subject, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot remove certificates with null or empty subject");
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Removing all {matches.Count} certificates with subject: {subject}");
                
                int removedCount = 0;
                foreach (var c in matches)
                { 
                    try 
                    { 
                        store.Remove(c);
                        removedCount++;
                    } 
                    catch (Exception ex) 
                    { 
                        logger?.Warning($"[Managed Identity] Failed to remove certificate: {ex.Message}");
                    }
                }

                logger?.Info($"[Managed Identity] Successfully removed {removedCount}/{matches.Count} certificates");
            }
            catch (Exception ex)
            { 
                logger?.Warning($"[Managed Identity] Certificate removal failed: {ex.Message}");
            }
        }

        internal static void RemoveByThumbprint(string thumbprint, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                logger?.Warning("[Managed Identity] Cannot remove certificate with null or empty thumbprint");
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var res = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                
                if (res.Count == 0)
                {
                    logger?.Info(() => $"[Managed Identity] No certificate found with thumbprint: {thumbprint}");
                    return;
                }
                
                logger?.Info(() => $"[Managed Identity] Removing certificate with thumbprint: {thumbprint}");
                
                foreach (var c in res)
                { 
                    try 
                    { 
                        store.Remove(c); 
                        logger?.Info(() => $"[Managed Identity] Successfully removed certificate with thumbprint: {thumbprint}");
                    } 
                    catch (Exception ex) 
                    { 
                        logger?.Warning($"[Managed Identity] Failed to remove certificate: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            { 
                logger?.Warning($"[Managed Identity] Certificate removal failed: {ex.Message}");
            }
        }

        internal static X509Certificate2 ResolveByThumbprintThenSubject(
            string thumbprint,
            string subject,
            bool cleanupOlder,
            out string resolvedThumbprint,
            ILoggerAdapter logger = null)
        {
            resolvedThumbprint = null;
            logger?.Verbose(() => $"[Managed Identity] Resolving certificate: thumbprint='{thumbprint}', subject='{subject}', cleanupOlder={cleanupOlder}");

            // 1) Try exact thumbprint match first
            X509Certificate2 exact = null;
            if (!string.IsNullOrWhiteSpace(thumbprint))
            {
                try
                {
                    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    var res = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                    exact = res.Count > 0 ? res[0] : null;
                    
                    if (exact == null)
                    {
                        logger?.Info(() => $"[Managed Identity] Certificate with thumbprint '{thumbprint}' not found, will fall back to subject");
                    }
                    else
                    {
                        logger?.Verbose(() => $"[Managed Identity] Found certificate with thumbprint '{thumbprint}', subject '{exact.Subject}', valid until {exact.NotAfter:u}");
                    }
                }
                catch (Exception ex)
                {
                    logger?.Warning($"[Managed Identity] Failed to read cert by thumbprint: {ex.Message}");
                }

                if (exact != null)
                {
                    var expiredBeyond = DateTime.UtcNow - exact.NotAfter.ToUniversalTime() > ExpiredPurgeWindow;
                    if (expiredBeyond)
                    {
                        logger?.Info(() => $"[Managed Identity] Certificate with thumbprint '{thumbprint}' is expired beyond purge window (expired on {exact.NotAfter:u}), removing and falling back to subject");
                        RemoveByThumbprint(exact.Thumbprint, logger);
                        exact = null;
                    }
                    else if (IsCurrentlyValid(exact))
                    {
                        logger?.Info(() => $"[Managed Identity] Using valid certificate with exact thumbprint match '{thumbprint}', valid until {exact.NotAfter:u}");
                        resolvedThumbprint = exact.Thumbprint;
                        return exact;
                    }
                    else
                    {
                        logger?.Info(() => $"[Managed Identity] Certificate with thumbprint '{thumbprint}' is expired (expired on {exact.NotAfter:u}), falling back to subject");
                    }
                }
            }

            // 2) Fall back to freshest by subject
            var freshest = GetFreshestBySubject(subject, logger);
            if (freshest != null)
            {
                if (cleanupOlder)
                {
                    logger?.Info(() => $"[Managed Identity] Cleaning up older certificates for subject '{subject}', keeping '{freshest.Thumbprint}'");
                    PruneOlder(subject, freshest.Thumbprint, logger);
                }
                
                if (IsCurrentlyValid(freshest))
                {
                    logger?.Info(() => $"[Managed Identity] Using valid certificate found by subject '{subject}', thumbprint '{freshest.Thumbprint}', valid until {freshest.NotAfter:u}");
                    resolvedThumbprint = freshest.Thumbprint;
                    return freshest;
                }
                else
                {
                    logger?.Info(() => $"[Managed Identity] Freshest certificate for subject '{subject}' is expired (expired on {freshest.NotAfter:u})");
                }
            }
            else
            {
                logger?.Info(() => $"[Managed Identity] No certificates found for subject '{subject}'");
            }

            logger?.Warning($"[Managed Identity] Failed to resolve any valid certificate by thumbprint '{thumbprint}' or subject '{subject}'");
            return null;
        }

        internal static void PurgeExpiredBeyondWindow(string subject, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot purge certificates with null or empty subject");
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var now = DateTime.UtcNow;
                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Checking {matches.Count} certificates with subject '{subject}' for expiration beyond {ExpiredPurgeWindow.TotalDays} days");
                
                int purgeCount = 0;
                foreach (var c in matches.OfType<X509Certificate2>())
                {
                    if (c.NotAfter.ToUniversalTime() < (now - ExpiredPurgeWindow))
                    {
                        try
                        { 
                            logger?.Verbose(() => $"[Managed Identity] Purging certificate {c.Thumbprint} expired beyond window (expired {(now - c.NotAfter.ToUniversalTime()).TotalDays:F1} days ago)");
                            store.Remove(c);
                            purgeCount++;
                        }
                        catch (Exception ex) 
                        { 
                            logger?.Warning($"[Managed Identity] Failed to purge expired certificate {c.Thumbprint}: {ex.Message}");
                        }
                    }
                }

                logger?.Info($"[Managed Identity] Purged {purgeCount} certificates expired beyond {ExpiredPurgeWindow.TotalDays} days");
            }
            catch (Exception ex)
            { 
                logger?.Warning($"[Managed Identity] Certificate purging failed: {ex.Message}");
            }
        }
    }
}
