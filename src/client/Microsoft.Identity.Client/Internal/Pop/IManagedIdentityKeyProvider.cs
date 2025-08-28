// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// Creates (or opens) the signing key used by Managed-Identity flows.
    /// If <see cref="KeyRequest.RequireAttestationToken"/> is <c>true</c> and
    /// the key is Key Guard-protected, the implementation also returns a fresh
    /// attestation token.
    /// </summary>
    internal interface IManagedIdentityKeyProvider
    {
        Task<KeyInfo> GetOrCreateKeyAsync(
            KeyRequest request,
            CancellationToken cancellationToken);
    }

    internal sealed class KeyRequest
    {
        public bool RequireAttestationToken { get; }
        public Uri AttestationUrl { get; }
        public string ClientId { get; }

        public KeyRequest(
            bool requireAttestationToken,
            Uri attestationUrl = null,
            string clientId = null)
        {
            RequireAttestationToken = requireAttestationToken;
            AttestationUrl = attestationUrl;
            ClientId = clientId;
        }
    }

    internal enum KeyType
    {
        KeyGuard,
        InMemory
    }

    internal sealed class AttestationToken
    {
        public string Jwt { get; }
        public DateTimeOffset ExpiresOn { get; }

        public AttestationToken(string jwt, DateTimeOffset expiresOn)
        {
            Jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            ExpiresOn = expiresOn;
        }
    }

    internal sealed class KeyInfo
    {
        /// <summary>Opaque identifier the attestor can use to look up / renew the key.</summary>
        public string KeyRef { get; }

        /// <summary>Hardware (<see cref="KeyType.KeyGuard"/>) or software (<see cref="KeyType.InMemory"/>).</summary>
        public KeyType KeyType { get; }

        /// <summary>e.g. 2048, 4096.</summary>
        public int KeyStrength { get; }

        /// <summary>
        /// Populated only when an attestation token was requested and
        /// the key is Key Guard-protected; otherwise <c>null</c>.
        /// </summary>
        public AttestationToken Token { get; }

        public KeyInfo(
            string keyRef,
            KeyType keyType,
            int keyStrength,
            AttestationToken token)
        {
            KeyRef = keyRef ?? throw new ArgumentNullException(nameof(keyRef));
            KeyType = keyType;
            KeyStrength = keyStrength;
            Token = token;
        }
    }
}
