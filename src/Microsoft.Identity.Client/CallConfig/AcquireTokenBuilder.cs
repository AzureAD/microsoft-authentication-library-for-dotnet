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
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.CallConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static class AcquireTokenBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        public static AcquireTokenByAuthorizationCodeParameterBuilder CreateForAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(scopes, authorizationCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static AcquireTokenForClientParameterBuilder CreateForClient(IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(scopes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static AcquireTokenInteractiveParameterBuilder CreateInteractive(IEnumerable<string> scopes)
        {
            return AcquireTokenInteractiveParameterBuilder.Create(scopes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="userAssertion"></param>
        /// <returns></returns>
        public static AcquireTokenOnBehalfOfParameterBuilder CreateOnBehalfOf(
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            return AcquireTokenOnBehalfOfParameterBuilder.Create(scopes, userAssertion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static AcquireTokenSilentParameterBuilder CreateSilent(IEnumerable<string> scopes, IAccount account)
        {
            return AcquireTokenSilentParameterBuilder.Create(scopes, account);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="deviceCodeResultCallback"></param>
        /// <returns></returns>
        public static AcquireTokenWithDeviceCodeParameterBuilder CreateWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(scopes, deviceCodeResultCallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static AcquireTokenWithIntegratedWindowsAuthParameterBuilder CreateWithIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenWithIntegratedWindowsAuthParameterBuilder.Create(scopes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static AcquireTokenWithUsernamePasswordParameterBuilder CreateWithUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            return AcquireTokenWithUsernamePasswordParameterBuilder.Create(scopes, username, password);
        }
    }
}