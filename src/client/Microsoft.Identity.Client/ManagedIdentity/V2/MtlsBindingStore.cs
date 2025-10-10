// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
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
        /// <summary>
        /// The time window after expiration during which certificates are still kept in the store.
        /// Once a certificate's expiration date is older than this window, it becomes eligible for purging.
        /// 7 days provides a balance between cleanup and troubleshooting needs.
        /// </summary>
        internal static readonly TimeSpan s_expiredPurgeWindow = TimeSpan.FromDays(7);

        /// <summary>
        /// Determines if a certificate is currently valid based on its expiration date.
        /// A certificate is valid if the current UTC time is before the certificate's NotAfter date.
        /// </summary>
        /// <param name="cert">The certificate to check for validity</param>
        /// <returns>True if the certificate is not null and its expiration date is in the future; otherwise, false</returns>
        /// <remarks>
        /// This method only checks expiration time and not other certificate validity aspects like
        /// revocation status or chain trust.
        /// </remarks>
        internal static bool IsCurrentlyValid(X509Certificate2 cert)
        {
            if (cert == null)
                return false;

            var now = DateTime.UtcNow;
            bool isValid = now >= cert.NotBefore.ToUniversalTime() &&
                          now < cert.NotAfter.ToUniversalTime();
            return isValid;
        }

        /// <summary>
        /// Determines if a certificate has passed its half-life point, which is the midpoint 
        /// between its NotBefore and NotAfter dates. Certificates beyond half-life are 
        /// candidates for proactive rotation.
        /// </summary>
        /// <param name="cert">The certificate to check</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <returns>
        /// True if the certificate is beyond its half-life or has an invalid validity period; 
        /// otherwise, false
        /// </returns>
        /// <remarks>
        /// Proactive rotation at half-life ensures new certificates are created well before
        /// expiration, reducing the risk of authentication failures during certificate transitions.
        /// </remarks>
        internal static bool IsBeyondHalfLife(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            // Null certificates are not beyond half-life (they're just invalid)
            if (cert == null)
                return false;

            // Get UTC-normalized validity dates
            var nb = cert.NotBefore.ToUniversalTime();
            var na = cert.NotAfter.ToUniversalTime();

            // Defensive check for invalid validity period (NotAfter <= NotBefore)
            if (na <= nb)
            {
                logger?.Warning($"[Managed Identity] Certificate has invalid validity period: NotBefore={nb}, NotAfter={na}");
                return true; // Treat as beyond half-life to trigger rotation
            }

            // Calculate half-life point and check if current time is beyond it
            var halfLife = nb + TimeSpan.FromTicks((na - nb).Ticks / 2);
            var isBeyond = DateTime.UtcNow >= halfLife;
            
            // Log detailed half-life info when relevant
            if (isBeyond && logger?.IsLoggingEnabled(LogLevel.Info) == true)
            {
                var now = DateTime.UtcNow;
                var timeUntilExpiry = na - now;
                logger.Info(() => $"[Managed Identity] Certificate {cert.Thumbprint} is beyond half-life. " +
                                  $"Valid: {nb:u} to {na:u}, Half-life: {halfLife:u}, Now: {now:u}, Time remaining: {timeUntilExpiry.TotalHours:F1} hours");
            }

            return isBeyond;
        }

        /// <summary>
        /// Verifies that a certificate's private key can be accessed and used for signing operations.
        /// This helps detect certificates with inaccessible or corrupted private keys.
        /// </summary>
        /// <param name="cert">The certificate to check</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <returns>
        /// True if the certificate has a usable RSA private key; otherwise, false
        /// </returns>
        /// <remarks>
        /// Private keys can become unusable after system reboots for certain key types,
        /// particularly with Windows KeyGuard-backed certificates. This method performs
        /// a minimal sign operation to verify the key is functional.
        /// </remarks>
        internal static bool IsPrivateKeyUsable(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
                return false;

            try
            {
                // Attempt to access the RSA private key
                using RSA rsa = cert.GetRSAPrivateKey();
                if (rsa == null)
                {
                    logger?.Info(() => $"[Managed Identity] Cert {cert.Thumbprint} has no RSA private key.");
                    return false;
                }

                // Perform a minimal signing operation to verify key usability
                // This doesn't export sensitive key material but confirms signing works
                var data = new byte[] { 0x42 };
                byte[] sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return sig != null && sig.Length > 0;
            }
            catch (Exception ex)
            {
                // Expected exceptions include:
                // - CryptographicException: When key access is denied or corrupted
                // - KeyNotFoundException: When key has been deleted or is inaccessible
                // - PlatformNotSupportedException: On platforms without proper crypto support
                logger?.Info(() => $"[Managed Identity] Private key unusable for cert {cert.Thumbprint}: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installs a certificate into the CurrentUser\My store, removing any existing duplicates
        /// with the same thumbprint first to prevent conflicts.
        /// </summary>
        /// <param name="cert">The certificate to install</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <returns>
        /// The subject of the installed certificate, or null if installation failed
        /// </returns>
        /// <remarks>
        /// This method handles duplicate removal to ensure clean installation and
        /// returns the subject for later lookup operations. The subject is returned even if
        /// store operations fail, as the in-memory certificate is still usable.
        /// </remarks>
        internal static string InstallAndGetSubject(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            // Validate input
            if (cert == null)
            {
                logger?.Warning("[Managed Identity] Cannot install null certificate");
                return null;
            }

            try
            {
                // Open certificate store with write access
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                logger?.Verbose(() => $"[Managed Identity] Installing certificate with thumbprint {cert.Thumbprint}, subject: {cert.Subject}, valid until: {cert.NotAfter:u}");

                // Find and remove any existing certificates with the same thumbprint
                // to prevent conflicts or duplicate entries
                var dupes = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
                if (dupes.Count > 0)
                {
                    logger?.Info(() => $"[Managed Identity] Removing {dupes.Count} duplicate certificate(s) with thumbprint {cert.Thumbprint}");
                    foreach (var d in dupes)
                    {
                        try
                        { 
                            store.Remove(d); 
                        }
                        catch (Exception ex)
                        {
                            // Continue with other certificates if one fails to be removed
                            logger?.Warning($"[Managed Identity] Failed to remove duplicate certificate: {ex.Message}");
                        }
                    }
                }

                // Add the new certificate to the store
                store.Add(cert);
                logger?.Info($"[Managed Identity] Successfully installed certificate with thumbprint {cert.Thumbprint}");
                return cert.Subject;
            }
            catch (Exception ex)
            {
                // Even if store operations fail, return the subject as the in-memory certificate is still usable
                logger?.Warning($"[Managed Identity] Failed to install binding cert: {ex.Message}");
                return cert.Subject;
            }
        }

        /// <summary>
        /// Retrieves the freshest (furthest expiration date) certificate with the specified subject
        /// from the CurrentUser\My store.
        /// </summary>
        /// <param name="subject">The subject distinguished name to search for</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <returns>
        /// The freshest matching certificate, or null if no matching certificates were found
        /// </returns>
        /// <remarks>
        /// This method doesn't filter by validity - it returns the certificate with the furthest
        /// expiration date even if already expired. Validity checking should be performed by the caller.
        /// </remarks>
        internal static X509Certificate2 GetFreshestBySubject(string subject, ILoggerAdapter logger = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot find certificates with null or empty subject");
                return null;
            }

            try
            {
                // Open certificate store in read-only mode
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                logger?.Verbose(() => $"[Managed Identity] Searching for certificates with subject: {subject}");

                // Find all certificates with the specified subject
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
            
                // Select the certificate with the furthest expiration date
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

        /// <summary>
        /// Removes older certificates with the same subject, keeping only the specified certificate
        /// and purging any certificates expired beyond the retention window.
        /// </summary>
        /// <param name="subject">The subject distinguished name to search for</param>
        /// <param name="keepThumbprint">The thumbprint of the certificate to keep</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <remarks>
        /// This implements the "foreground" certificate rotation strategy where only the newest 
        /// certificate is kept and older ones are removed. This contrasts with the "background" 
        /// strategy which preserves valid certificates during their validity period.
        /// </remarks>
        internal static void PruneOlder(string subject, string keepThumbprint, ILoggerAdapter logger = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(keepThumbprint))
            {
                logger?.Warning($"[Managed Identity] Cannot prune with null/empty subject or thumbprint. Subject: '{subject}', Thumbprint: '{keepThumbprint}'");
                return;
            }

            try
            {
                // Open certificate store with write access
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var now = DateTime.UtcNow;
                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Pruning certificates: found {matches.Count} with subject '{subject}', keeping thumbprint '{keepThumbprint}'");
            
                int removedCount = 0;
                int expiredCount = 0;

                // Examine each certificate with the specified subject
                foreach (var c in matches.OfType<X509Certificate2>())
                {
                    // Determine if this certificate should be kept or removed
                    var isKeep = string.Equals(c.Thumbprint, keepThumbprint, StringComparison.OrdinalIgnoreCase);
                    var expiredBeyondWindow = c.NotAfter.ToUniversalTime() < (now - s_expiredPurgeWindow);

                    // Remove if not the specified certificate or if expired beyond the retention window
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
                            // Continue with other certificates if one fails to be removed
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

        /// <summary>
        /// Removes all certificates with the specified subject from the CurrentUser\My store.
        /// </summary>
        /// <param name="subject">The subject distinguished name to search for</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <remarks>
        /// This is a complete cleanup operation that removes all certificates associated with an identity,
        /// regardless of validity status or expiration date.
        /// </remarks>
        internal static void RemoveAllBySubject(string subject, ILoggerAdapter logger = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot remove certificates with null or empty subject");
                return;
            }

            try
            {
                // Open certificate store with write access
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Find all certificates with the specified subject
                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Removing all {matches.Count} certificates with subject: {subject}");
            
                int removedCount = 0;
                // Remove each matching certificate
                foreach (var c in matches)
                { 
                    try 
                    { 
                        store.Remove(c);
                        removedCount++;
                    } 
                    catch (Exception ex) 
                    { 
                        // Continue with other certificates if one fails to be removed
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

        /// <summary>
        /// Removes a specific certificate identified by its thumbprint from the CurrentUser\My store.
        /// </summary>
        /// <param name="thumbprint">The thumbprint of the certificate to remove</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <remarks>
        /// This is a targeted removal operation for a specific certificate. Unlike subject-based
        /// removal, this method guarantees only the exact specified certificate will be removed.
        /// </remarks>
        internal static void RemoveByThumbprint(string thumbprint, ILoggerAdapter logger = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                logger?.Warning("[Managed Identity] Cannot remove certificate with null or empty thumbprint");
                return;
            }

            try
            {
                // Open certificate store with write access
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Find the certificate with the specified thumbprint
                var res = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            
                if (res.Count == 0)
                {
                    logger?.Info(() => $"[Managed Identity] No certificate found with thumbprint: {thumbprint}");
                    return;
                }
            
                logger?.Info(() => $"[Managed Identity] Removing certificate with thumbprint: {thumbprint}");
            
                // Remove each matching certificate (should be at most one)
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

        /// <summary>
        /// Resolves a certificate using a multi-tiered strategy:
        /// 1. First try by exact thumbprint match
        /// 2. If not found or expired, fall back to freshest certificate by subject
        /// 
        /// This method applies intelligent resolution rules including expiration checking
        /// and optional cleanup of older certificates.
        /// </summary>
        /// <param name="thumbprint">The preferred certificate thumbprint to look for</param>
        /// <param name="subject">The subject to fall back to if thumbprint lookup fails</param>
        /// <param name="cleanupOlder">Whether to remove older certificates with the same subject</param>
        /// <param name="resolvedThumbprint">Output parameter for the resolved certificate's thumbprint</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <returns>
        /// The resolved certificate if found and valid, or null if no valid certificate could be found
        /// </returns>
        /// <remarks>
        /// This is the main certificate resolution method used by the managed identity client.
        /// It implements the certificate lookup strategy with fallbacks and lifecycle management.
        /// </remarks>
        internal static X509Certificate2 ResolveByThumbprintThenSubject(
            string thumbprint,
            string subject,
            bool cleanupOlder,
            out string resolvedThumbprint,
            ILoggerAdapter logger = null)
        {
            resolvedThumbprint = null;
            logger?.Verbose(() => $"[Managed Identity] Resolving certificate: thumbprint='{thumbprint}', subject='{subject}', cleanupOlder={cleanupOlder}");

            // STRATEGY 1: Try exact thumbprint match first (most precise)
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

                // If found by thumbprint, check expiration status
                if (exact != null)
                {
                    // Check if expired beyond our retention window (very stale)
                    var expiredBeyond = DateTime.UtcNow - exact.NotAfter.ToUniversalTime() > s_expiredPurgeWindow;
                    if (expiredBeyond)
                    {
                        // Very stale certificate - remove it and fall back to subject search
                        logger?.Info(() => $"[Managed Identity] Certificate with thumbprint '{thumbprint}' is expired beyond purge window (expired on {exact.NotAfter:u}), removing and falling back to subject");
                        RemoveByThumbprint(exact.Thumbprint, logger);
                        exact = null;
                    }
                    else if (IsCurrentlyValid(exact))
                    {
                        // Certificate is currently valid - use it
                        logger?.Info(() => $"[Managed Identity] Using valid certificate with exact thumbprint match '{thumbprint}', valid until {exact.NotAfter:u}");
                        resolvedThumbprint = exact.Thumbprint;
                        return exact;
                    }
                    else
                    {
                        // Certificate is expired but within retention window - fall back to subject
                        logger?.Info(() => $"[Managed Identity] Certificate with thumbprint '{thumbprint}' is expired (expired on {exact.NotAfter:u}), falling back to subject");
                    }
                }
            }

            // STRATEGY 2: Fall back to freshest by subject (less precise but more resilient)
            var freshest = GetFreshestBySubject(subject, logger);
            if (freshest != null)
            {
                // Optionally clean up older certificates with the same subject
                if (cleanupOlder)
                {
                    logger?.Info(() => $"[Managed Identity] Cleaning up older certificates for subject '{subject}', keeping '{freshest.Thumbprint}'");
                    PruneOlder(subject, freshest.Thumbprint, logger);
                }
                
                // Check if the freshest certificate is valid
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

            // No valid certificate found through either strategy
            logger?.Warning($"[Managed Identity] Failed to resolve any valid certificate by thumbprint '{thumbprint}' or subject '{subject}'");
            return null;
        }

        /// <summary>
        /// Purges certificates that have expired beyond the retention window.
        /// Unlike PruneOlder, this method only removes very stale certificates and
        /// preserves all valid and recently expired certificates.
        /// </summary>
        /// <param name="subject">The subject distinguished name to search for</param>
        /// <param name="logger">Optional logger to record diagnostic information</param>
        /// <remarks>
        /// This implements the "background" certificate rotation strategy where valid certificates
        /// are preserved, even if they're not the newest. This allows for smoother transitions
        /// during certificate rotation.
        /// </remarks>
        internal static void PurgeExpiredBeyondWindow(string subject, ILoggerAdapter logger = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(subject))
            {
                logger?.Warning("[Managed Identity] Cannot purge certificates with null or empty subject");
                return;
            }

            try
            {
                // Open certificate store with write access
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var now = DateTime.UtcNow;
                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                logger?.Info(() => $"[Managed Identity] Checking {matches.Count} certificates with subject '{subject}' for expiration beyond {s_expiredPurgeWindow.TotalDays} days");
            
                int purgeCount = 0;
                // Check each certificate for expiration beyond retention window
                foreach (var c in matches.OfType<X509Certificate2>())
                {
                    // Only remove certificates expired beyond our retention window
                    if (c.NotAfter.ToUniversalTime() < (now - s_expiredPurgeWindow))
                    {
                        try
                        { 
                            logger?.Verbose(() => $"[Managed Identity] Purging certificate {c.Thumbprint} expired beyond window (expired {(now - c.NotAfter.ToUniversalTime()).TotalDays:F1} days ago)");
                            store.Remove(c);
                            purgeCount++;
                        }
                        catch (Exception ex) 
                        { 
                            // Continue with other certificates if one fails to be removed
                            logger?.Warning($"[Managed Identity] Failed to purge expired certificate {c.Thumbprint}: {ex.Message}");
                        }
                    }
                }
            
                logger?.Info($"[Managed Identity] Purged {purgeCount} certificates expired beyond {s_expiredPurgeWindow.TotalDays} days");
            }
            catch (Exception ex)
            { 
                logger?.Warning($"[Managed Identity] Certificate purging failed: {ex.Message}");
            }
        }
    }
}
