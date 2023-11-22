// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.netstandard
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class NullKeyMaterialManager : IKeyMaterialManager
    {
        private readonly X509Certificate2 _bindingCertificate;
        private readonly ILoggerAdapter _logger;

        public NullKeyMaterialManager(ILoggerAdapter logger)
        {
            _logger = logger;

            _bindingCertificate = null;
        }

        public X509Certificate2 BindingCertificate => _bindingCertificate;

        CryptoKeyType IKeyMaterialManager.CryptoKeyType => CryptoKeyType.None;

        public bool IsBindingCertificateExpired()
        {
            return true;
        }

        public TimeSpan GetTimeUntilCertificateExpiration()
        {
            return TimeSpan.Zero;
        }

        public bool IsKeyGuardProtected()
        {
            return false;
        }

        public bool CertificateHasPrivateKey()
        {
            return false;
        }
    }
}
