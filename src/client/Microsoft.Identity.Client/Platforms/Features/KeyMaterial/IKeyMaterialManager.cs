// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    internal interface IKeyMaterialManager
    {
        ECDsaCng CredentialKey { get; }
        CryptoKeyType CryptoKeyType { get; }
        ECDsaCng GetCngKey();

        bool IsKeyGuardProtected(CngKey cngKey);

        void DetermineKeyType(CngKey cngKey);

    }
}
