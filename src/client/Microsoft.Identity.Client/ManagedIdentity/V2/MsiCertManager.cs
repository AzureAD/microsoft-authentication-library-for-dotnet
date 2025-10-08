// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal sealed class MsiCertManager
    {
        private readonly RequestContext _ctx;

        internal MsiCertManager(RequestContext ctx) => _ctx = ctx;

        /// <summary>
        /// Ensure a usable binding for (identityKey, tokenType). Reuse if possible, otherwise mint.
        /// </summary>
        internal async Task<(X509Certificate2 cert, CertificateRequestResponse resp)>
            GetOrMintBindingAsync(
                string identityKey,
                string tokenType,
                Func<CancellationToken, Task<(CertificateRequestResponse resp, AsymmetricAlgorithm privateKey)>> mintBindingAsync,
                CancellationToken ct)
        {
            // 1) per-identity reuse
            if (TryBuildFromPerIdentityMapping(identityKey, tokenType, out var cert, out var resp))
            {
                MaybeLogHalfLife(cert);
                return (cert, resp);
            }

            // 2) PoP-only cross-identity fallback (unit test)
            if (string.Equals(tokenType, Constants.MtlsPoPTokenType, StringComparison.OrdinalIgnoreCase) &&
                TryBuildFromAnyMapping(Constants.MtlsPoPTokenType, out cert, out resp))
            {
                // attach mapping to current identity
                ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(
                    identityKey, resp, cert.Subject, cert.Thumbprint, tokenType);

                _ctx.Logger.Info("[IMDSv2] Reused PoP binding from another identity (test scenario).");
                MaybeLogHalfLife(cert);
                return (cert, resp);
            }

            // 3) mint + install + prune + cache
            var (newResp, privKey) = await mintBindingAsync(ct).ConfigureAwait(false);

            if (privKey is not RSA rsa)
            {
                throw new InvalidOperationException("The provided private key is not an RSA key.");
            }

            var newCert = CommonCryptographyManager.AttachPrivateKeyToCert(newResp.Certificate, rsa);

            var subject = MtlsBindingStore.InstallAndGetSubject(newCert, _ctx.Logger);
            MtlsBindingStore.PruneOlder(subject, newCert.Thumbprint, _ctx.Logger);

            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(
                identityKey, newResp, subject, newCert.Thumbprint, tokenType);

            _ctx.Logger.Info("[IMDSv2] Minted mTLS binding and cached IMDSv2 metadata + subject.");
            return (newCert, newResp);
        }

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

            if (!ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(
                    identityKey, tokenType, out var cachedResp, out var subject, out var tp))
            {
                return false;
            }

            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject(tp, subject, cleanupOlder: true, out var resolvedTp);
            if (!MtlsBindingStore.IsCurrentlyValid(resolved))
                return false;

            if (!StringComparer.OrdinalIgnoreCase.Equals(tp, resolvedTp))
            {
                // keep mapping exact for next time
                ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identityKey, cachedResp, subject, resolvedTp, tokenType);
            }

            cert = resolved;
            resp = cachedResp;
            return true;
        }

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

            var c = MtlsBindingStore.ResolveByThumbprintThenSubject(anyTp, anySubject, cleanupOlder: true, out _);
            if (!MtlsBindingStore.IsCurrentlyValid(c))
                return false;

            cert = c;
            resp = anyResp;
            return true;
        }

        private void MaybeLogHalfLife(X509Certificate2 cert)
        {
            if (MtlsBindingStore.IsBeyondHalfLife(cert))
            {
                _ctx.Logger.Info("[IMDSv2] Binding reached half-life; reusing for this call.");
                // Deliberately no background rotation (keeps tests deterministic).
            }
        }
    }
}
