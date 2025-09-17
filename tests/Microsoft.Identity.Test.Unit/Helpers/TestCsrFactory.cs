// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Test.Unit.Helpers
{
    internal class TestCsrFactory : ICsrFactory
    {
        public (string csrPem, RSA privateKey) Generate(RSA rsa, string clientId, string tenantId, CuidInfo cuId)
        {
            // we don't care about the RSA that's passed in, we will always return the same mock private key
            return ("mock-csr", CreateMockRsa());
        }

        /// <summary>
        /// Creates a mock <see cref="RSA"/> private key for testing purposes by loading key parameters from an XML string.
        /// The XML format is used because it allows all necessary RSA parameters to be embedded directly in the code,
        /// enabling deterministic and repeatable test runs. This method returns an <see cref="RSA"/> object rather than a string,
        /// as cryptographic operations in tests require a usable key instance, not just its serialized representation.
        /// </summary>
        public static RSA CreateMockRsa()
        {
            RSA rsa = null;

#if NET462 || NET472
            // .NET Framework runs only on Windows, so RSACng (Windows-only) is always available
            rsa = new RSACng();
#else
            // Cross-platform .NET - RSA.Create() returns appropriate PSS-capable implementation
            rsa = RSA.Create();
#endif
            rsa.FromXmlString(TestConstants.XmlPrivateKey);
            return rsa;
        }
    }
}
