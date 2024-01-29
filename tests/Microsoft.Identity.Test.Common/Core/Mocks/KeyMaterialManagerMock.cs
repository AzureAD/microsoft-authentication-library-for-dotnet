// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    /// <summary>
    /// Mock implementation of the IKeyMaterialManager interface for testing purposes.
    /// </summary>
    internal class KeyMaterialManagerMock : IKeyMaterialManager
    {
        /// <summary>
        /// Initializes a new instance of the KeyMaterialManagerMock class.
        /// </summary>
        /// <param name="bindingCertificate">The X509 certificate used for binding.</param>
        /// <param name="cryptoKeyType">The type of cryptographic key used.</param>
        public KeyMaterialManagerMock(X509Certificate2 bindingCertificate, CryptoKeyType cryptoKeyType)
        {
            BindingCertificate = bindingCertificate;
            CryptoKeyType = cryptoKeyType;
        }

        /// <summary>
        /// Gets the X509 certificate used for binding.
        /// </summary>
        public X509Certificate2 BindingCertificate { get; }

        /// <summary>
        /// Gets the type of cryptographic key used.
        /// </summary>
        public CryptoKeyType CryptoKeyType { get; }
    }
}
