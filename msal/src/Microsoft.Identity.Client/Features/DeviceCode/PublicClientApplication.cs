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

namespace Microsoft.Identity.Client
{
    public sealed partial class PublicClientApplication : ClientApplicationBase
    {
        // TODO: removing from public API until end to end testing is completed, add this back in when done.
        ///// <summary>
        ///// Acquires device code from the authority and returns it to the caller via
        ///// the deviceCodeResultCallback. The function then proceeds to poll for the security
        ///// token which is granted upon successful login based on the device code information.
        ///// See https://aka.ms/msal-device-code-flow.
        ///// </summary>
        ///// <param name="scopes">Scopes requested to access a protected API</param>
        ///// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        ///// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        ///// <returns></returns>
        //public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
        //    IEnumerable<string> scopes,
        //    string extraQueryParameters,
        //    Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        //{
        //    if (deviceCodeResultCallback == null)
        //    {
        //        throw new ArgumentNullException("A deviceCodeResultCallback must be provided for Device Code authentication to work properly");
        //    }

        //    return await AcquireTokenWithDeviceCodeAsync(
        //        scopes,
        //        extraQueryParameters,
        //        deviceCodeResultCallback,
        //        CancellationToken.None).ConfigureAwait(false);
        //}

        ///// <summary>
        ///// Acquires device code from the authority and returns it to the caller via
        ///// the deviceCodeResultCallback. The function then proceeds to poll for the security
        ///// token which is granted upon successful login based on the device code information.
        ///// See https://aka.ms/msal-device-code-flow.
        ///// </summary>
        ///// <param name="scopes">Scopes requested to access a protected API</param>
        ///// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        ///// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        ///// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        ///// <returns></returns>
        //public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
        //    IEnumerable<string> scopes,
        //    string extraQueryParameters,
        //    Func<DeviceCodeResult, Task> deviceCodeResultCallback,
        //    CancellationToken cancellationToken)
        //{
        //    Authority authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);

        //    var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
        //    requestParams.ExtraQueryParameters = extraQueryParameters;

        //    var handler = new DeviceCodeRequest(requestParams, deviceCodeResultCallback);
        //    return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        //}
    }
}
