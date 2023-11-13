// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    internal interface IKeyMaterialManager
    {
        ECDsaCng GetCngKey(string keyProviderName, string keyName);

        ECDsaCng ECDsaCngKey { get; }

        CryptoKeyType CryptoKeyType { get; }
        
    }
}
