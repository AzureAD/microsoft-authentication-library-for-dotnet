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
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal static class RequestValidationHelper
    {
#if DESKTOP || NETSTANDARD1_3 || NET_CORE
        /// <summary>
        /// Determines whether or not the cached client assertion can be used again for the next authentication request by checking it's
        /// values against incoming request parameters.
        /// </summary>
        /// <param name="clientAssertionParameters">Incoming client assertion parameters containing cached client assertion</param>
        /// <returns>Returns true if the previously cached client assertion is valid</returns>
        public static bool ValidateClientAssertion(AuthenticationRequestParameters clientAssertionParameters)
        {
            if (clientAssertionParameters.ClientCredential == null)
            {
                throw new ArgumentException("The " + nameof(clientAssertionParameters) + " does not contain the ClientCredential");
            }
            else if (string.IsNullOrWhiteSpace(clientAssertionParameters.ClientCredential.Assertion))
            {
                return false;
            }

            //Check if all current client assertion values match incoming parameters and expiration time
            //The clientCredential object contains the previously used values in the cached client assertion string
            var clientCredential = clientAssertionParameters.ClientCredential;
            var expired = (clientCredential.ValidTo <=
                                        Jwt.JsonWebToken.ConvertToTimeT(DateTime.UtcNow +
                                                                        TimeSpan.FromMinutes(
                                                                        Constants
                                                                        .ExpirationMarginInMinutes)));

            var parametersMatch = clientCredential.Audience == clientAssertionParameters.Authority.SelfSignedJwtAudience
                && clientCredential.ContainsX5C == clientAssertionParameters.SendCertificate;

            return !expired && parametersMatch;
        }
#endif
    }
}
