// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Ignore Spelling: Mtls

using System;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Microsoft.Identity.Client.Internal.Pop;   // KeyGuardKey helper

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Provides a cached POP key, either a Key Guard–protected one or a software key.
    /// </summary>
    internal static class PopKeyProvider
    {
        private static readonly object _sync = new();
        private static PopKey _cached;

        /// <summary>
        /// Returns the cached key or creates one synchronously (thread‑safe,
        /// “first winner” pattern).
        /// </summary>
        public static PopKey Get()
        {
            if (_cached != null)
                return _cached;

            lock (_sync)
            {
                if (_cached != null)
                    return _cached;

                _cached = TryCreateKeyGuardKey() ??
                          new PopKey(RSA.Create(), isKeyGuard: false);

                return _cached;
            }
        }

        // ──────────────────────────  helpers  ──────────────────────────

        private static PopKey TryCreateKeyGuardKey()
        {
            try
            {
                // Open existing KG key or create a fresh one.
                CngKey kg;
                try
                {
                    kg = CngKey.Open("KeyGuardRSAKey",
                                     new CngProvider("Microsoft Software Key Storage Provider"));
                }
                catch (CryptographicException)
                {
                    kg = KeyGuardKey.CreateFresh();
                }

                if (KeyGuardKey.IsKeyGuardProtected(kg))
                {
                    return new PopKey(kg);
                }

                kg.Dispose();
            }
            catch
            {
                /* Any failure means we fall back to software key */
            }

            return null;
        }
    }

    /// <summary>
    /// Lightweight container describing the POP key and exposing its native
    /// handle when available.
    /// </summary>
    internal sealed class PopKey : IDisposable
    {
        public bool IsKeyGuard { get; }

        /// <remarks>
        /// Non‑null only when <see cref="IsKeyGuard"/> is <c>true</c>.
        /// </remarks>
        public SafeNCryptKeyHandle SafeHandle { get; }

        public RSA Rsa { get; }

        // Hardware‑key ctor
        internal PopKey(CngKey kgKey)
        {
            IsKeyGuard = true;
            Rsa = new RSACng(kgKey);
            SafeHandle = kgKey.Handle;
        }

        // Software‑key ctor
        internal PopKey(RSA rsa, bool isKeyGuard)
        {
            IsKeyGuard = isKeyGuard;
            Rsa = rsa;
            SafeHandle = null;
        }

        public void Dispose() => Rsa?.Dispose();
    }
}
