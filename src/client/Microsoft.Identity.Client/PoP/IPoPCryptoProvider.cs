// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PoP
{
    // TODO: we can expose this interface to users for a simple but low-level extensibility mechanism.
    // For a more complex extensibility mechanism, we should allow users to configure SigningCredentials, 
    // the top level object exposed by Wilson. Wilson then has adapters for certificates, KeyVault etc.

    /// <summary>
    /// An abstraction over an the asymmetric key operations needed by POP, that encapsulates a pair of 
    /// public and private keys and some typical crypto operations.
    /// All symetric operations are SHA256
    /// </summary>
    /// <remarks>
    /// Ideally there should be a single public / private key pair associated with a machine, so implementers of this interface
    /// should consider exposing a singleton.
    /// </remarks>
    internal interface IPoPCryptoProvider
    {
        /// <summary>
        /// The cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// </summary>
        string CannonicalPublicKeyJwk { get; }

        /// <summary>
        /// Signs the byte array using the private key
        /// </summary>
        byte[] Sign(byte[] data);
    }
}
