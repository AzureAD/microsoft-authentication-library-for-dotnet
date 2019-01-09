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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static class PublicClientApplicationExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            this IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes, 
            object parent)
        {
            return AcquireTokenInteractiveParameterBuilder.Create(publicClientApplication, scopes, parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static AcquireTokenSilentPcaParameterBuilder AcquireTokenSilent(
            this IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes, 
            IAccount account)
        {
            return AcquireTokenSilentPcaParameterBuilder.Create(publicClientApplication, scopes, account);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="deviceCodeResultCallback"></param>
        /// <returns></returns>
        public static AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            this IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(publicClientApplication, scopes, deviceCodeResultCallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static AcquireTokenWithIntegratedWindowsAuthParameterBuilder AcquireTokenWithIntegratedWindowsAuth(
            this IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes)
        {
            return AcquireTokenWithIntegratedWindowsAuthParameterBuilder.Create(publicClientApplication, scopes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static AcquireTokenWithUsernamePasswordParameterBuilder AcquireTokenWithUsernamePassword(
            this IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            return AcquireTokenWithUsernamePasswordParameterBuilder.Create(publicClientApplication, scopes, username, password);
        }
    }
}