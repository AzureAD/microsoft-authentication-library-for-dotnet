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
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Internal
{
    internal class ClientKey
    {
        public ClientKey(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            this.ClientId = clientId;
            this.HasCredential = false;
        }

        public ClientKey(string clientId, ClientCredential clientCredential, Authenticator authenticator)
            : this(clientId)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException("clientCredential");
            }

            this.Authenticator = authenticator;
            this.Credential = clientCredential;
            this.HasCredential = true;
        }

        public ClientKey(string clientId, ClientAssertion clientAssertion) : this(clientId)
        {
            if (clientAssertion == null)
            {
                throw new ArgumentNullException("clientAssertion");
            }

            this.Assertion = clientAssertion;
            this.HasCredential = true;
        }

        public ClientCredential Credential { get; }
        public ClientAssertion Assertion { get; }
        public Authenticator Authenticator { get; }
        public string ClientId { get; }
        public bool HasCredential { get; private set; }

        public void AddToParameters(IDictionary<string, string> parameters)
        {
            if (this.ClientId != null)
            {
                parameters[OAuth2Parameter.ClientId] = this.ClientId;
            }

            if (this.Credential != null)
            {
                if (!string.IsNullOrEmpty(this.Credential.Secret))
                {
                    parameters[OAuth2Parameter.ClientSecret] = this.Credential.Secret;
                }
                else
                {
                    ClientAssertion clientAssertion = this.Credential.ClientAssertion;

                    if (clientAssertion == null || this.Credential.ValidTo != 0)
                    {
                        bool assertionNearExpiry = (this.Credential.ValidTo <=
                                                    JsonWebToken.ConvertToTimeT(DateTime.UtcNow +
                                                                                TimeSpan.FromMinutes(
                                                                                    Constant.ExpirationMarginInMinutes)));
                        if (assertionNearExpiry)
                        {
                            JsonWebToken jwtToken = new JsonWebToken(this.ClientId,
                                this.Authenticator.SelfSignedJwtAudience);
                            clientAssertion = jwtToken.Sign(this.Credential.Certificate);
                            this.Credential.ValidTo = jwtToken.Payload.ValidTo;
                            this.Credential.ClientAssertion = clientAssertion;
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = clientAssertion.AssertionType;
                    parameters[OAuth2Parameter.ClientAssertion] = clientAssertion.Assertion;
                }
            }

            else if (this.Assertion != null)
            {
                parameters[OAuth2Parameter.ClientAssertionType] = this.Assertion.AssertionType;
                parameters[OAuth2Parameter.ClientAssertion] = this.Assertion.Assertion;
            }
        }
    }
}