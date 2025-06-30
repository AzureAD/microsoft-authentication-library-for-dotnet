// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.Internal.Credential
{
    internal sealed class CredentialStore
    {
        private X509Certificate2 _cert;
        private ManagedIdentityCredentialResponse _cred;
        private static readonly TimeSpan s_buffer = TimeSpan.FromMinutes(60);

        internal bool TryGetValid(out X509Certificate2 cert,
                                  out ManagedIdentityCredentialResponse cred)
        {
            bool ok = _cert != null && _cert.NotAfter - s_buffer > DateTime.UtcNow;
            cert = ok ? _cert : null;
            cred = ok ? _cred : null;
            return ok;
        }

        internal void Save(X509Certificate2 cert, ManagedIdentityCredentialResponse cred)
        {
            _cert = cert;
            _cred = cred;
        }

        internal static X509Certificate2 Import(string b64) =>
            new(Convert.FromBase64String(b64), (SecureString)null,
                X509KeyStorageFlags.MachineKeySet);
    }
}
