// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Internal.Credential
{
    // ------------------------------------------------------------------------
    // replace with real implementations when available
    // ------------------------------------------------------------------------
    internal sealed class KeyMaterial
    {
        internal RSA Rsa { get; }
        internal KeyMaterial(RSA rsa) => Rsa = rsa;
    }
}
