// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Credential
{
    internal interface IKeyMaterialInfo
    {
        ECDsaCng GetMachineKey(string keyProviderName, string keyName);
        
        bool IsKeyGuardProtected(CngKey cngKey);

        CryptoKeyType CryptoKeyType { get; }
        
    }
}
