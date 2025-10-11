// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Manages certificates for Mutual TLS authentication with Azure Managed Service Identities (MSI).
    /// This class handles certificate retrieval, creation, reuse, and cross-process rotation coordination.
    /// </summary>
    /// <remarks>
    /// Strategy:
    ///  1) Reuse per-identity binding if valid (preferred).
    ///  2) For PoP only, optionally reuse binding from any identity (test support).
    ///  3) Mint when missing.
    /// Rotation:
    ///  - If we reuse a cert at/after half-life → schedule proactive rotation (background).
    ///  - Rotation uses a cross-process named mutex + stable jitter so only one process mints.
    ///  - Do NOT delete the existing valid binding (A). Only purge certs expired > 7 days.
    /// </remarks>
    internal sealed class MsiCertManager
    {
        private static Random s_random = new Random();

        private readonly RequestContext _ctx;

        /// <summary>
        /// Initializes a new instance of the MsiCertManager with the specified request context.
        /// </summary>
        /// <param name="ctx">The request context containing logging and service dependencies</param>
        internal MsiCertManager(RequestContext ctx) => _ctx = ctx;

        /// <summary>
        /// Obtains a certificate for mTLS binding, either from cache or by minting a new one.
        /// Implements a tiered strategy for certificate retrieval with proactive rotation.
        /// </summary>
        /// <param name="identityKey">The identity key (client ID) of the managed identity</param>
        /// <param name="tokenType">The token type (Bearer or PoP)</param>
        /// <param name="mintBindingAsync">Function to mint a new certificate when needed</param>
        /// <param name="ct">Cancellation token for async operations</param>
        /// <returns>A tuple containing the certificate and its associated metadata response</returns>
        internal async Task<(X509Certificate2 cert, CertificateRequestResponse resp)>
            GetOrMintBindingAsync(
                string identityKey,
                string tokenType,
                Func<CancellationToken, Task<(CertificateRequestResponse resp, 
                    AsymmetricAlgorithm privateKey)>> mintBindingAsync,
                CancellationToken ct)
        {
            // 1) Reuse from in-memory mapping (with rehydration fallback)
            if (TryBuildFromPerIdentityMapping(identityKey, tokenType, out var cert, out var resp))
            {
                if (MtlsBindingStore.IsBeyondHalfLife(cert))
                {
                    _ctx.Logger.Info("[IMDSv2] Binding reached half-life; reusing for this call, scheduling proactive rotation.");
                    ScheduleProactiveRotation(identityKey, tokenType, mintBindingAsync);
                }
                return (cert, resp);
            }

            // 2) PoP-only cross-identity reuse (test support)
            if (string.Equals(tokenType, Constants.MtlsPoPTokenType, StringComparison.OrdinalIgnoreCase) &&
                TryBuildFromAnyMapping(Constants.MtlsPoPTokenType, out cert, out resp))
            {
                ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(
                    identityKey, resp, cert.Subject, cert.Thumbprint, tokenType);

                _ctx.Logger.Info("[IMDSv2] Reused PoP binding from another identity (test scenario).");

                if (MtlsBindingStore.IsBeyondHalfLife(cert))
                {
                    _ctx.Logger.Info("[IMDSv2] Reused PoP binding is at/after half-life; scheduling proactive rotation.");
                    ScheduleProactiveRotation(identityKey, tokenType, mintBindingAsync);
                }
                return (cert, resp);
            }

            // 3) Mint + install + prune (foreground path keeps only the newest for this subject)
            var (newResp, privKey) = await mintBindingAsync(ct).ConfigureAwait(false);

            if (privKey is not RSA rsa)
                throw new InvalidOperationException("The provided private key is not an RSA key.");

            var newCert = CommonCryptographyManager.AttachPrivateKeyToCert(newResp.Certificate, rsa);

            // Persist friendly name (best-effort) so other processes can rehydrate later
            TrySetFriendlyName(newCert, identityKey, tokenType, newResp);

            var subject = MtlsBindingStore.InstallAndGetSubject(newCert, _ctx.Logger);

            // Foreground path keeps only this newest binding + purges stale (>7d after expiry)
            MtlsBindingStore.PruneOlder(subject, newCert.Thumbprint, _ctx.Logger);

            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(
                identityKey, newResp, subject, newCert.Thumbprint, tokenType);

            _ctx.Logger.Info("[IMDSv2] Minted mTLS binding and cached IMDSv2 metadata + subject.");
            return (newCert, newResp);
        }

        /// <summary>
        /// Background rotation: jitter + named mutex. Keeps prior binding (A) while valid.
        /// </summary>
        private void ScheduleProactiveRotation(
            string identityKey,
            string tokenType,
            Func<CancellationToken, Task<(CertificateRequestResponse resp, AsymmetricAlgorithm privateKey)>> mintBindingAsync)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var delay = ComputeStableJitter();
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay).ConfigureAwait(false);

                    using var mutex = TryAcquireNamedMutex(identityKey, tokenType);
                    if (mutex == null)
                    {
                        _ctx.Logger.Verbose(() => "[IMDSv2] Another process is already rotating the binding; skipping.");
                        return;
                    }

                    try
                    {
                        var (resp, privKey) = await mintBindingAsync(CancellationToken.None).ConfigureAwait(false);
                        if (privKey is not RSA rsa)
                            return;

                        var cert = CommonCryptographyManager.AttachPrivateKeyToCert(resp.Certificate, rsa);

                        // Tag the cert for store rehydration (best-effort)
                        TrySetFriendlyName(cert, identityKey, tokenType, resp);

                        var subject = MtlsBindingStore.InstallAndGetSubject(cert, _ctx.Logger);

                        // Background path: DO NOT delete valid A. Only purge very stale ones.
                        MtlsBindingStore.PurgeExpiredBeyondWindow(subject, _ctx.Logger);

                        ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(
                            identityKey, resp, subject, cert.Thumbprint, tokenType);

                        _ctx.Logger.Info("[IMDSv2] Proactively rotated mTLS binding at half-life (kept prior binding until expiry).");
                    }
                    finally
                    {
                        try
                        { mutex.ReleaseMutex(); }
                        catch { /* best effort */ }
                    }
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Info(() => $"[IMDSv2] Proactive certificate rotation failed: {ex.GetType().Name}: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Creates a deterministic jitter delay from identity information.
        /// This ensures multiple processes don't all try to rotate at exactly the same moment,
        /// while maintaining stability (same input always produces same delay).
        /// </summary>
        /// <returns>A TimeSpan representing the jitter delay</returns>
        private static TimeSpan ComputeStableJitter()
        {
            int jitter = s_random.Next(-Constants.DefaultJitterRangeInSeconds, Constants.DefaultJitterRangeInSeconds);
            return TimeSpan.FromSeconds(jitter);
        }

        /// <summary>
        /// Attempts to acquire a named mutex for cross-process coordination of certificate rotation.
        /// 
        /// A mutex (mutual exclusion) is a synchronization primitive that ensures only one process
        /// can execute a critical section of code at a time, preventing race conditions when
        /// multiple processes access shared resources.
        /// 
        /// In this certificate management scenario, the named mutex prevents multiple processes from
        /// simultaneously rotating certificates for the same identity, which could lead to:
        /// 1. Wasted resources from redundant certificate generation
        /// 2. Potential certificate conflicts or inconsistent state
        /// 3. Unnecessary load on the certificate authority
        /// 
        /// The method first attempts to create a Global mutex (visible across all user sessions),
        /// then falls back to a Local mutex (visible only within current session) if Global fails.
        /// The mutex name is derived from the identity key and token type, ensuring separate
        /// coordination for different identities.
        /// 
        /// The caller must release the mutex by calling ReleaseMutex() when finished with
        /// certificate rotation operations.
        /// </summary>
        /// <param name="identityKey">Identity key used to create unique mutex name</param>
        /// <param name="tokenType">Token type used to create unique mutex name</param>
        /// <returns>
        /// An acquired Mutex if successful, or null if acquisition failed
        /// (indicating another process is already handling rotation)
        /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.mutex
        /// </returns>
        private static Mutex TryAcquireNamedMutex(string identityKey, string tokenType)
        {
            // Create a sanitized suffix from identity info to ensure valid mutex name
            // This prevents illegal characters in mutex names while maintaining uniqueness
            string suffix = Sanitize(identityKey) + "_" + Sanitize(tokenType);
            
            // Try Global namespace first - visible across all user sessions on the machine
            // This provides the widest scope of coordination between all processes
            var globalName = @"Global\MSAL_MI_mTLS_ROT_" + suffix;

            if (TryOpenAndLock(globalName, out var mGlobal))
                return mGlobal;  // Successfully acquired Global mutex

            // Fall back to Local namespace - visible only within current user session
            // This works in restricted environments where Global mutex creation might be denied
            var localName = @"Local\MSAL_MI_mTLS_ROT_" + suffix;
            if (TryOpenAndLock(localName, out var mLocal))
                return mLocal;  // Successfully acquired Local mutex

            // Could not acquire either mutex - another process likely holds it
            return null;

            // Helper method to try opening and immediately locking a mutex
            // Returns true only if mutex was successfully created AND acquired
            static bool TryOpenAndLock(string name, out Mutex m)
            {
                m = null;
                try
                {
                    // Create mutex (initial state unlocked)
                    m = new Mutex(false, name);
                    
                    // Try to acquire it with 0 timeout (non-blocking)
                    // WaitOne returns true if mutex was acquired, false if already owned
                    if (m.WaitOne(0))
                        return true;  // Successfully acquired

                    // Mutex exists but is owned by another process
                    // Clean up and return false
                    m.Dispose();
                    m = null;
                    return false;
                }
                catch  // Handle access denied or other mutex-related exceptions
                {
                    // Clean up on any error and return false
                    m?.Dispose();
                    m = null;
                    return false;
                }
            }

            // Helper method to sanitize strings for mutex name creation
            // Mutex names have character restrictions across platforms
            static string Sanitize(string s)
            {
                if (string.IsNullOrEmpty(s))
                    return "na";  // Default value for empty inputs
                    
                var sb = new StringBuilder(s.Length);
                foreach (var ch in s)
                {
                    // Only allow alphanumeric chars, hyphen, and underscore
                    // This ensures mutex name validity across platforms
                    if ((ch >= 'A' && ch <= 'Z') ||
                        (ch >= 'a' && ch <= 'z') ||
                        (ch >= '0' && ch <= '9') ||
                        ch == '-' || ch == '_')
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        // Replace any disallowed character with underscore
                        sb.Append('_');
                    }
                }
                
                // Truncate if too long (mutex names have length limitations)
                return sb.Length > 64 ? sb.ToString(0, 64) : sb.ToString();
            }
        }

        /// <summary>
        /// Attempts to set the FriendlyName property on a certificate to enable cross-process rehydration.
        /// The FriendlyName contains encoded metadata about the identity, token type, and endpoints.
        /// </summary>
        /// <param name="cert">The certificate to set the FriendlyName on</param>
        /// <param name="identityKey">The identity key to encode</param>
        /// <param name="tokenType">The token type to encode</param>
        /// <param name="resp">Certificate response data to encode</param>
        private void TrySetFriendlyName(X509Certificate2 cert, string identityKey, string tokenType, CertificateRequestResponse resp)
        {
            try
            {
                var fn = BindingMetadataPersistence
                    .BuildFriendlyName(identityKey, tokenType, resp);
                if (!string.IsNullOrEmpty(fn))
                {
                    cert.FriendlyName = fn; // best-effort (may be unsupported on non-Windows)
                }
            }
            catch
            {
                // ignore: friendly name is best-effort
            }
        }

        /// <summary>
        /// Attempts to retrieve a certificate and response for the specific identity and token type.
        /// First checks in-memory cache, then falls back to store rehydration via FriendlyName.
        /// </summary>
        /// <param name="identityKey">The identity key to look up</param>
        /// <param name="tokenType">The token type to look up</param>
        /// <param name="cert">Output parameter for the retrieved certificate</param>
        /// <param name="resp">Output parameter for the certificate metadata</param>
        /// <returns>True if a valid certificate was found; otherwise, false</returns>
        private bool TryBuildFromPerIdentityMapping(
            string identityKey,
            string tokenType,
            out X509Certificate2 cert,
            out CertificateRequestResponse resp)
        {
            cert = null;
            resp = null;

            if (string.IsNullOrEmpty(identityKey))
                return false;

            bool foundInMemory = ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(
                identityKey, tokenType, out var cachedResp, out var subject, out var tp);
            
            // If not in memory, try rehydration (consolidated check)
            if (!foundInMemory && !BindingMetadataPersistence.TryRehydrateFromStore(
                identityKey, tokenType, _ctx.Logger, out cachedResp, out subject, out tp))
            {
                return false;
            }

            // If rehydrated (not found in memory), cache for future lookups
            if (!foundInMemory)
                ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identityKey, cachedResp, subject, tp, tokenType);

            // Common resolution logic
            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject(tp, subject, cleanupOlder: true, out var resolvedTp, _ctx.Logger);
            if (!MtlsBindingStore.IsCurrentlyValid(resolved))
                return false;

            // When machine reboots the KeyGuard key may become unusable
            // this will ensure we delete the x509 cert and mint a new one
            if (!MtlsBindingStore.IsPrivateKeyUsable(resolved, _ctx.Logger))
            {
                _ctx.Logger.Info($"[IMDSv2] Binding cert {resolved.Thumbprint} has unusable private key. Removing and minting fresh.");

                try
                {
                    MtlsBindingStore.RemoveByThumbprint(resolved.Thumbprint, _ctx.Logger);
                }
                catch { }
                return false;
            }

            // Update cache if thumbprint changed
            if (!StringComparer.OrdinalIgnoreCase.Equals(tp, resolvedTp))
                ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identityKey, cachedResp, subject, resolvedTp, tokenType);

            cert = resolved;
            resp = cachedResp;
            return true;
        }

        /// <summary>
        /// For PoP tokens only, attempts to find any valid certificate from any identity.
        /// This is primarily used for test scenarios or when sharing certificates across identities.
        /// </summary>
        /// <param name="tokenType">The token type (must be PoP)</param>
        /// <param name="cert">Output parameter for the retrieved certificate</param>
        /// <param name="resp">Output parameter for the certificate metadata</param>
        /// <returns>True if a valid certificate was found; otherwise, false</returns>
        private bool TryBuildFromAnyMapping(
            string tokenType,
            out X509Certificate2 cert,
            out CertificateRequestResponse resp)
        {
            cert = null;
            resp = null;

            if (!ImdsV2ManagedIdentitySource.TryGetAnyImdsV2BindingMetadata(
                    tokenType, out var anyResp, out var anySubject, out var anyTp))
            {
                return false;
            }

            var c = MtlsBindingStore.ResolveByThumbprintThenSubject(anyTp, anySubject, cleanupOlder: true, out _, _ctx.Logger);
            if (!MtlsBindingStore.IsCurrentlyValid(c))
                return false;

            // When machine reboots the KeyGuard key may become unusable
            // this will ensure we delete the x509 cert and mint a new one
            if (!MtlsBindingStore.IsPrivateKeyUsable(c, _ctx.Logger))
            {
                _ctx.Logger.Info($"[IMDSv2] Borrowed binding cert {c.Thumbprint} has unusable private key. Removing and minting fresh.");
                
                try
                { 
                    MtlsBindingStore.RemoveByThumbprint(c.Thumbprint, _ctx.Logger); 
                }
                catch { }
                return false;
            }

            cert = c;
            resp = anyResp;
            return true;
        }
    }
}
