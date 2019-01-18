// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal static class ClientCredentialHelper
    {
#if DESKTOP || NETSTANDARD1_3 || NET_CORE
        /// <summary>
        ///     Determines whether or not the cached client assertion can be used again for the next authentication request by
        ///     checking it's
        ///     values against incoming request parameters.
        /// </summary>
        /// <returns>Returns true if the previously cached client assertion is valid</returns>
        public static bool ValidateClientAssertion(ClientCredential clientCredential, AuthorityEndpoints endpoints, bool sendX5C)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException(nameof(clientCredential));
            }
            else if (string.IsNullOrWhiteSpace(clientCredential.Assertion))
            {
                return false;
            }

            //Check if all current client assertion values match incoming parameters and expiration time
            //The clientCredential object contains the previously used values in the cached client assertion string
            bool expired = clientCredential.ValidTo <=
                           JsonWebToken.ConvertToTimeT(
                               DateTime.UtcNow + TimeSpan.FromMinutes(Constants.ExpirationMarginInMinutes));

            bool parametersMatch = clientCredential.Audience == endpoints?.SelfSignedJwtAudience &&
                                   clientCredential.ContainsX5C == sendX5C;

            return !expired && parametersMatch;
        }

        public static Dictionary<string, string> CreateClientCredentialBodyParameters(
            ICoreLogger logger, 
            ICryptographyManager cryptographyManager, 
            ClientCredential clientCredential,
            string clientId,
            AuthorityEndpoints endpoints, 
            bool sendX5C)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (clientCredential != null)
            {
                if (!string.IsNullOrEmpty(clientCredential.Secret))
                {
                    parameters[OAuth2Parameter.ClientSecret] = clientCredential.Secret;
                }
                else
                {
                    if (clientCredential.Assertion == null || clientCredential.ValidTo != 0)
                    {
                        if (!ValidateClientAssertion(clientCredential, endpoints, sendX5C))
                        {
                            logger.Info("Client Assertion does not exist or near expiry.");
                            var jwtToken = new JsonWebToken(cryptographyManager, clientId, endpoints?.SelfSignedJwtAudience);
                            clientCredential.Assertion = jwtToken.Sign(clientCredential.Certificate, sendX5C);
                            clientCredential.ValidTo = jwtToken.Payload.ValidTo;
                            clientCredential.ContainsX5C = sendX5C;
                            clientCredential.Audience = endpoints?.SelfSignedJwtAudience;
                        }
                        else
                        {
                            logger.Info("Reusing the unexpired Client Assertion...");
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                    parameters[OAuth2Parameter.ClientAssertion] = clientCredential.Assertion;
                }
            }
            return parameters;
        }
#endif
    }
}