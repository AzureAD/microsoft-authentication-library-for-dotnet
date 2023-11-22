// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface IKeyMaterialManager
    {
        CryptoKeyType CryptoKeyType { get; }

        X509Certificate2 BindingCertificate { get; }
    }
}
