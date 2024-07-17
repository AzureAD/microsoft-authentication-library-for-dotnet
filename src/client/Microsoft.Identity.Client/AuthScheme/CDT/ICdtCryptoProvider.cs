// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AuthScheme.CDT
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICdtCryptoProvider
    {
        /// <summary>
        /// The canonical representation of the JWK.         
        /// See https://tools.ietf.org/html/rfc7638#section-3
        /// </summary>
        string CannonicalPublicKeyJwk { get; }

        /// <summary>
        /// Algorithm used to sign proof of possession request. 
        /// See <see href="https://learn.microsoft.com/azure/key-vault/keys/about-keys-details#signverify">EC algorithms</see> for ECD.
        /// See <see href="https://learn.microsoft.com/azure/key-vault/keys/about-keys-details#signverify-1">RSA algorithms</see> for RSA.
        /// </summary>
        string CryptographicAlgorithm { get; }

        /// <summary>
        /// Signs the byte array using the private key
        /// </summary>
        byte[] Sign(byte[] data);
    }
}
