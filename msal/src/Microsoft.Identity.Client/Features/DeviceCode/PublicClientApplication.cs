//------------------------------------------------------------------------------
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Features.DeviceCode;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client
{
    public sealed partial class PublicClientApplication : ClientApplicationBase
    {
        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeAsync(scopes, string.Empty, deviceCodeResultCallback);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of passing extra parameters. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            if (deviceCodeResultCallback == null)
            {
                throw new ArgumentNullException("A deviceCodeResultCallback must be provided for Device Code authentication to work properly");
            }

            return await AcquireTokenWithDeviceCodeAsync(
                scopes,
                extraQueryParameters,
                deviceCodeResultCallback,
                CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            return AcquireTokenWithDeviceCodeAsync(scopes, string.Empty, deviceCodeResultCallback, cancellationToken);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of passing extra query parameters and cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);

            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler = new DeviceCodeRequest(
                HttpManager,
                CryptographyManager,
                TelemetryManager,
                requestParams,
                ApiEvent.ApiIds.None,
                deviceCodeResultCallback);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
