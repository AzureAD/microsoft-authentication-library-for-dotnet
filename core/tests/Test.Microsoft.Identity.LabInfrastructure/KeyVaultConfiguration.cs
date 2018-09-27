//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

namespace Test.Microsoft.Identity.LabInfrastructure
{
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// The URL of the Key Vault instance.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The authentication type to use to communicate with the Key Vault.
        /// </summary>
        public KeyVaultAuthenticationType AuthType { get; set; }

        /// <summary>
        /// The ID of the test harness client.
        /// </summary>
        /// <remarks>
        /// This should be configured as having access to the Key Vault instance specified at <see cref="Url"/>.
        /// </remarks>
        public string ClientId { get; set; }

        /// <summary>
        /// The thumbprint of the <see cref="System.Security.Cryptography.X509Certificates.X509Certificate2"/> to use when
        /// <see cref="AuthType"/> is <see cref="KeyVaultAuthenticationType.ClientCertificate"/>.
        /// </summary>
        public string CertThumbprint { get; set; }
    }

    public enum KeyVaultAuthenticationType
    {
        ClientCertificate,
        UserCredential
    }
}
