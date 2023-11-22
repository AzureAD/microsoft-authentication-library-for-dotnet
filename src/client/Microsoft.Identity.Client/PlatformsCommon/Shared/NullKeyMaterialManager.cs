// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class NullKeyMaterialManager(ILoggerAdapter logger) : IKeyMaterialManager
    {
        public ILoggerAdapter Logger { get; } = logger;
        public X509Certificate2 BindingCertificate => null;
        CryptoKeyType IKeyMaterialManager.CryptoKeyType => CryptoKeyType.None;
    }
}
