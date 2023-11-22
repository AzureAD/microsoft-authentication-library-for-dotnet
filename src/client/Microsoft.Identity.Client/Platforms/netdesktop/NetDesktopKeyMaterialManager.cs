// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class NetDesktopKeyMaterialManager(ILoggerAdapter logger) : IKeyMaterialManager
    {
        public ILoggerAdapter Logger { get; } = logger;
        public X509Certificate2 BindingCertificate => null;
        CryptoKeyType IKeyMaterialManager.CryptoKeyType => CryptoKeyType.None;
    }
}
