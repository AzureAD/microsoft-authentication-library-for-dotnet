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

using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Containing certificate used to create client assertion.
    /// </summary>
    public sealed class ClientAssertionCertificate : IClientAssertionCertificate
    {
        private string clientId = null;

        /// <summary>
        /// Constructor to create credential with client Id and certificate.
        /// </summary>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="certificate">The certificate used as credential.</param>
        public ClientAssertionCertificate(string clientId, X509Certificate2 certificate)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (certificate.GetRSAPublicKey().KeySize < MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException("certificate",
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.CertificateKeySizeTooSmallTemplate, MinKeySizeInBits));
            }

            this.clientId = clientId;
            this.Certificate = certificate;
        }


        /// <summary>
        /// Gets the identifier of the client requesting the token.
        /// </summary>
        public string ClientId { get { return clientId; } }

        /// <summary>
        /// Gets minimum X509 certificate key size in bits
        /// </summary>
        public static int MinKeySizeInBits
        {
            get { return 2048; }
        }

        /// <summary>
        /// Gets the certificate used as credential.
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Signs a message using the private key in the certificate
        /// </summary>
        /// <param name="message">Message that needs to be signed</param>
        /// <returns>Signed message as a byte array</returns>
        public byte[] Sign(string message)
        {
            CryptographyHelper helper = new CryptographyHelper();
            return helper.SignWithCertificate(message, this.Certificate);
        }

        /// <summary>
        /// Returns thumbprint of the certificate
        /// </summary>
        public string Thumbprint
        {
            // Thumbprint should be url encoded
            get { return Base64UrlEncoder.Encode(this.Certificate.GetCertHash()); }
        }
    }
}
