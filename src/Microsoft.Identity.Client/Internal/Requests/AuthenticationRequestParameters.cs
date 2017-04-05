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
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal partial class AuthenticationRequestParameters
    {
        public RequestContext RequestContext { get; set; }

        public Authority Authority { get; set; }

        public TokenCache TokenCache { get; set; }

        public SortedSet<string> Scope { get; set; }

        public string ClientId { get; set; }

        public string AuthorizationCode { get; set; }

        public Uri RedirectUri { get; set; }

        public string LoginHint { get; set; }

        public string ExtraQueryParameters { get; set; }

        public string Prompt { get; set; }

        public IUser User { get; set; }

        public UserAssertion UserAssertion { get; set; }

#if DESKTOP || NETSTANDARD1_3
        public ClientCredential ClientCredential { get; set; }

        public bool HasCredential => (ClientCredential != null);

#endif
        public IDictionary<string, string> ToParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();
#if DESKTOP || NETSTANDARD1_3
            if (ClientCredential != null)
            {
                if (!string.IsNullOrEmpty(ClientCredential.Secret))
                {
                    parameters[OAuth2Parameter.ClientSecret] = ClientCredential.Secret;
                }
                else
                {
                    if (ClientCredential.Assertion == null || ClientCredential.ValidTo != 0)
                    {
                        bool assertionNearExpiry = (ClientCredential.ValidTo <=
                                                    JsonWebToken.ConvertToTimeT(DateTime.UtcNow +
                                                                                TimeSpan.FromMinutes(
                                                                                    Constants.ExpirationMarginInMinutes)));
                        if (assertionNearExpiry)
                        {
                            JsonWebToken jwtToken = new JsonWebToken(ClientId,
                                Authority.SelfSignedJwtAudience);
                            ClientCredential.Assertion = jwtToken.Sign(ClientCredential.Certificate);
                            ClientCredential.ValidTo = jwtToken.Payload.ValidTo;
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                    parameters[OAuth2Parameter.ClientAssertion] = ClientCredential.Assertion;
                }
            }
#endif
            return parameters;
        }
    }
}