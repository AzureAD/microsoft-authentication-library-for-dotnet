// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Internal.Credential
{
    internal static class KeyGuardHelper
    {
        internal static KeyMaterial TryCreateKeyGuardKey() => null;

        internal static KeyMaterial CreateRsaKey()
        {
            RSA rsa = RSA.Create();
            return new KeyMaterial(rsa);
        }
    }
}
