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

using System.Collections.Generic;

namespace Microsoft.Identity.Client.ApiConfig
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// </summary>
    public static class ConfidentialClientApplicationExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        public static AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenForAuthorizationCode(
            this IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(
                confidentialClientApplication,
                scopes,
                authorizationCode);
        }

        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            this IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(confidentialClientApplication, scopes);
        }

        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="userAssertion"></param>
        /// <returns></returns>
        public static AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(
            this IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            return AcquireTokenOnBehalfOfParameterBuilder.Create(confidentialClientApplication, scopes, userAssertion);
        }

        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static AcquireTokenSilentCcaParameterBuilder AcquireTokenSilent(
            this IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes,
            IAccount account)
        {
            return AcquireTokenSilentCcaParameterBuilder.Create(confidentialClientApplication, scopes, account);
        }
    }
#endif
}