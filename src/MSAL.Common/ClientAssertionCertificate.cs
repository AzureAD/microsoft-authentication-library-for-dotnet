//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Containing certificate used to create client assertion.
    /// </summary>
    public sealed class ClientAssertionCertificate : IClientAssertionCertificate
    {
        /// <summary>
        /// Constructor to create credential using certificate.
        /// </summary>
        /// <param name="certificate">The certificate used as credential.</param>
        public ClientAssertionCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (certificate.PublicKey.Key.KeySize < MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException("certificate",
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CertificateKeySizeTooSmallTemplate, MinKeySizeInBits));
            }
            
            this.Certificate = certificate;
        }

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

        public byte[] Sign(string message)
        {
            CryptographyHelper helper = new CryptographyHelper();
            return helper.SignWithCertificate(message, this.Certificate);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Thumbprint
        {
            // Thumbprint should be url encoded
            get { return Base64UrlEncoder.Encode(this.Certificate.Thumbprint); }
        }
    }
}
