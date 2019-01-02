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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.CallConfig;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME // Hide confidential client on mobile platforms

    public partial class ConfidentialClientApplication
    {
        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenOnBehalfOfParameters parameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(parameters, UserTokenCache);
            requestParams.UserAssertion = parameters.UserAssertion;
            requestParams.SendCertificate = parameters.WithOnBehalfOfCertificate;
            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope); // TODO: consolidate this with parameters...

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenByAuthorizationCodeParameters parameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(parameters, UserTokenCache);
            requestParams.AuthorizationCode = parameters.AuthorizationCode;
            requestParams.SendCertificate = false;
            var handler = new AuthorizationCodeRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope);  // TODO: consolidate this appropriately
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenForClientParameters parameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(parameters, AppTokenCache);
            requestParams.IsClientCredentialRequest = true;
            requestParams.SendCertificate = parameters.WithForClientCertificate;
            var handler = new ClientCredentialRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope, // todo: consolidate this appropriately
                parameters.ForceRefresh);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(
            IGetAuthorizationRequestUrlParameters parameters,
            CancellationToken cancellationToken)
        {
            var requestParameters = CreateRequestParameters(parameters, UserTokenCache);
            if (!string.IsNullOrWhiteSpace(parameters.RedirectUri))
            {
                // TODO: should we wire up redirect uri override across the board and put this in the CreateRequestParameters method?
                requestParameters.RedirectUri = new Uri(parameters.RedirectUri);
            }

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParameters,
                ApiEvent.ApiIds.None,
                parameters.ExtraScopesToConsent,
                parameters.LoginHint,
                UIBehavior.SelectAccount,
                null);

            // todo: need to pass through cancellation token here
            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }
    }
#endif
}