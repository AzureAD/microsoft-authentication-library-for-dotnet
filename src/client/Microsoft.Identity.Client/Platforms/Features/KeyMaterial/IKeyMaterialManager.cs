// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    internal interface IKeyMaterialManager
    {
        CryptoKeyType CryptoKeyType { get; }

        X509Certificate2 BindingCertificate { get; }

        bool IsBindingCertificateExpired();
        TimeSpan GetTimeUntilCertificateExpiration();

        bool IsKeyGuardProtected(); // Check if the key is KeyGuard protected
        bool CertificateHasPrivateKey(); // Check if the binding certificate has a private key
    }
}
