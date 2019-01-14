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
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
    public partial class ConfidentialClientApplication
    {
        internal ConfidentialClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();
            AppTokenCacheInternal = new TokenCache(ServiceBundle);
        }

        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        public AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenForAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(
                this,
                scopes,
                authorizationCode);
        }

        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(this, scopes);
        }

        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="userAssertion"></param>
        /// <returns></returns>
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            return AcquireTokenOnBehalfOfParameterBuilder.Create(this, scopes, userAssertion);
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public GetAuthorizationRequestUrlParameterBuilder GetAuthorizationRequestUrl(
            IEnumerable<string> scopes)
        {
            return GetAuthorizationRequestUrlParameterBuilder.Create(this, scopes);
        }

        async Task<AuthenticationResult> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenByAuthorizationCodeParameters authorizationCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(authorizationCodeParameters, UserTokenCacheInternal);
            requestParams.AuthorizationCode = authorizationCodeParameters.AuthorizationCode;
            requestParams.SendCertificate = false;
            var handler = new AuthorizationCodeRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope);  // TODO(migration): consolidate this appropriately
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);        }

        async Task<AuthenticationResult> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(clientParameters, AppTokenCacheInternal);
            requestParams.IsClientCredentialRequest = true;
            requestParams.SendCertificate = clientParameters.SendX5C;
            var handler = new ClientCredentialRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope, // todo(migration): consolidate this appropriately
                clientParameters.ForceRefresh);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(onBehalfOfParameters, UserTokenCacheInternal);
            requestParams.UserAssertion = onBehalfOfParameters.UserAssertion;
            requestParams.SendCertificate = onBehalfOfParameters.WithOnBehalfOfCertificate;
            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope); // TODO(migration): consolidate this with parameters...

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<Uri> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IGetAuthorizationRequestUrlParameters authorizationRequestUrlParameters,
            CancellationToken cancellationToken)
        {
            var requestParameters = CreateRequestParameters(authorizationRequestUrlParameters, UserTokenCacheInternal);
            if (!string.IsNullOrWhiteSpace(authorizationRequestUrlParameters.RedirectUri))
            {
                // TODO(migration): should we wire up redirect uri override across the board and put this in the CreateRequestParameters method?
                requestParameters.RedirectUri = new Uri(authorizationRequestUrlParameters.RedirectUri);
            }

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParameters,
                ApiEvent.ApiIds.None,
                authorizationRequestUrlParameters.ExtraScopesToConsent,
                authorizationRequestUrlParameters.LoginHint,
                UIBehavior.SelectAccount,
                null);

            // todo: need to pass through cancellation token here
            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }
    }
#endif
}