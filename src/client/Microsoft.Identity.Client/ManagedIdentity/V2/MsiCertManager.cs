// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal sealed class MsiCertManager
    {
        private readonly IMtlsCertCache _certCache;
        private readonly IBindingCache _bindingCache;
        private readonly ICertificateRepository _repo;

        // Avoids repeated store checks/installs per (tenant, MI, tokenType) in this process
        private static readonly ConcurrentDictionary<MiCacheKey, byte> s_storeEnsured =
            new ConcurrentDictionary<MiCacheKey, byte>();

        private static readonly ConcurrentDictionary<MiCacheKey, SemaphoreSlim> s_mintGates = 
            new ConcurrentDictionary<MiCacheKey, SemaphoreSlim>();

        // (Optional) test hook
        internal static void ResetStoreEnsureFlagsForTest() => s_storeEnsured.Clear();

        public MsiCertManager(IMtlsCertCache certCache, IBindingCache bindingCache, ICertificateRepository repo = null)
        {
            _certCache = certCache ?? throw new ArgumentNullException(nameof(certCache));
            _bindingCache = bindingCache ?? throw new ArgumentNullException(nameof(bindingCache));
            _repo = repo ?? MtlsBindingStore.Default;
        }

        public async Task<(X509Certificate2 cert, CertificateRequestResponse resp)> GetOrMintBindingAsync(
            string identityKey,
            string tenantId,
            string managedIdentityId,
            string tokenType,
            Func<CancellationToken, Task<(CertificateRequestResponse resp, AsymmetricAlgorithm privateKey)>> mintBindingAsync,
            CancellationToken ct)
        {
            var memKey = MiCacheKey.FromStrings(tenantId, managedIdentityId, tokenType);

            // ---- 1) Per-key in-process cache (strict hit) ----
            if (_certCache.TryGetLatest(memKey, DateTimeOffset.UtcNow, out var cached))
            {
                var cachedResp = (CertificateRequestResponse)cached.IssueCredentialResponse;
                var cachedCert = cached.Certificate;

                _bindingCache.Cache(identityKey, tokenType, cachedResp, cachedCert.Subject);

                // Self-heal the store once per key in this process
                EnsureStoreEntryOnce(memKey, managedIdentityId, tenantId, tokenType, cachedCert, cachedResp);
                return (cachedCert, cachedResp);
            }

            var gate = s_mintGates.GetOrAdd(memKey, _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Re-check after entering the gate (another waiter may have populated the cache)
                if (_certCache.TryGetLatest(memKey, DateTimeOffset.UtcNow, out cached))
                {
                    var cachedResp2 = (CertificateRequestResponse)cached.IssueCredentialResponse;
                    var cachedCert2 = cached.Certificate;

                    _bindingCache.Cache(identityKey, tokenType, cachedResp2, cachedCert2.Subject);
                    EnsureStoreEntryOnce(memKey, managedIdentityId, tenantId, tokenType, cachedCert2, cachedResp2);
                    return (cachedCert2, cachedResp2);
                }

                // ---- 1b) Subject-level fallback (same process, same CN/DC) ----
                if (_certCache is InMemoryMtlsCertCache concreteCache &&
                    concreteCache.TryGetLatestBySubject(
                        subjectCn: managedIdentityId,
                        subjectDc: tenantId,
                        tokenType: tokenType,
                        now: DateTimeOffset.UtcNow,
                        out var subjEntry))
                {
                    var respFromMem = (CertificateRequestResponse)subjEntry.IssueCredentialResponse;
                    var certFromMem = subjEntry.Certificate;

                    // Re-index it under the current key so next lookup is O(1)
                    _certCache.Put(memKey, subjEntry);
                    _bindingCache.Cache(identityKey, tokenType, respFromMem, certFromMem.Subject);

                    // Self-heal the store once per key
                    EnsureStoreEntryOnce(memKey, managedIdentityId, tenantId, tokenType, certFromMem, respFromMem);
                    return (certFromMem, respFromMem);
                }

                // ---- 2) Cross-process rehydrate from store ----
                if (_repo.TryResolveFreshestBySubjectAndType(
        subjectCn: managedIdentityId,
        subjectDc: tenantId,
        tokenType: tokenType,
        cert: out var storeCert,
        mtlsEndpoint: out var endpoint))
                {
                    // capture thumbprint before dispose so we can purge if it’s unusable
                    string thumb = null;
                    try
                    { thumb = storeCert.Thumbprint; }
                    catch { /* ignore */ }

                    var detached = TryCreateDetachedWithSameKey(storeCert);
                    storeCert.Dispose();

                    if (detached != null)
                    {
                        var respFromStore = new CertificateRequestResponse
                        {
                            ClientId = managedIdentityId,
                            TenantId = tenantId,
                            MtlsAuthenticationEndpoint = endpoint
                        };

                        var entryFromStore = new MtlsCertCacheEntry(
                            certificate: detached,
                            issueCredentialResponse: respFromStore,
                            keyHandle: string.Empty,
                            createdAtUtc: DateTimeOffset.UtcNow);

                        _certCache.Put(memKey, entryFromStore);
                        _bindingCache.Cache(identityKey, tokenType, respFromStore, detached.Subject);
                        return (detached, respFromStore);
                    }

                    // No usable private key: purge the stale public-only cert so future rehydrate doesn't keep hitting it
                    if (!string.IsNullOrEmpty(thumb))
                    {
                        try
                        { _repo.TryRemoveByThumbprintIfUnusable(thumb); }
                        catch { /* best effort */ }
                    }
                    // fall through to mint…
                }

                // ---- 3) Mint new (CSR -> /issuecredential) ONLY IF all above missed ----
                var (resp, privateKey) = await mintBindingAsync(ct).ConfigureAwait(false);
                var rsaPrivateKey = privateKey as RSA ?? throw new InvalidOperationException("The provided private key is not an RSA key.");

                var cert = CommonCryptographyManager.AttachPrivateKeyToCert(resp.Certificate, rsaPrivateKey);

                var entry = new MtlsCertCacheEntry(cert, resp, keyHandle: string.Empty, createdAtUtc: DateTimeOffset.UtcNow);
                _certCache.Put(memKey, entry);
                _bindingCache.Cache(identityKey, tokenType, resp, cert.Subject);

                // FriendlyName tagging and install (best-effort)
                try
                {
                    var friendly = BindingFriendlyName.Build(tokenType, resp.MtlsAuthenticationEndpoint);
                    _repo.TryInstallWithFriendlyName(cert, friendly);
                }
                catch { /* best-effort */ }

                return (cert, resp);
            }
            finally
            {
                gate.Release();
                // Do NOT remove the gate here; keeping it avoids a race that could bypass queuing.
                // The number of keys is naturally bounded (tenant, MI, tokenType).
            }
        }

        private void EnsureStoreEntryOnce(
            MiCacheKey memKey,
            string managedIdentityId,
            string tenantId,
            string tokenType,
            X509Certificate2 cert,
            CertificateRequestResponse resp)
        {
            if (!s_storeEnsured.TryAdd(memKey, 0))
                return;

            try
            {
                if (!_repo.TryResolveFreshestBySubjectAndType(
                        subjectCn: managedIdentityId,
                        subjectDc: tenantId,
                        tokenType: tokenType,
                        cert: out var _,
                        mtlsEndpoint: out var _))
                {
                    var friendly = BindingFriendlyName.Build(tokenType, resp.MtlsAuthenticationEndpoint);
                    _repo.TryInstallWithFriendlyName(cert, friendly);
                }
            }
            catch
            {
                // best-effort — ignore
            }
        }

        private static X509Certificate2 TryCreateDetachedWithSameKey(X509Certificate2 storeCert)
        {
            try
            {
                // Get the live private key (KeyGuard-backed RSA/CNG)
                using (var rsa = storeCert.GetRSAPrivateKey())
                {
                    if (rsa == null)
                    {
                        return null;
                    }
                    // Rebuild a public-only instance from raw DER, then bind the existing private key
                    var rawB64 = Convert.ToBase64String(storeCert.RawData);
                    return CommonCryptographyManager.AttachPrivateKeyToCert(rawB64, rsa);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
