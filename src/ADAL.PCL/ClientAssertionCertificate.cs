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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Containing certificate used to create client assertion.
    /// </summary>
    public sealed class ClientAssertionCertificate
    {
        /// <summary>
        /// Constructor to create credential with client Id and certificate.
        /// </summary>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="certificate">The certificate used as credential.</param>
        /// <param name="password">The certificate password</param>
        public ClientAssertionCertificate(string clientId, byte[] certificate, string password)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("password");
            }

            this.ClientId = clientId;
            this.Certificate = certificate;
            this.Password = password;
        }

        /// <summary>
        /// Gets minimum X509 certificate key size in bits
        /// </summary>
        public static int MinKeySizeInBits
        {
            get { return 2048; }
        }

        /// <summary>
        /// Gets the identifier of the client requesting the token.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets the certificate used as credential.
        /// </summary>
        public byte[] Certificate { get; private set; }

        public string Password { get; private set; }

        internal byte[] Sign(string message)
        {
            return PlatformPlugin.CryptographyHelper.SignWithCertificate(message, this.Certificate, this.Password);
        }
    }
}
