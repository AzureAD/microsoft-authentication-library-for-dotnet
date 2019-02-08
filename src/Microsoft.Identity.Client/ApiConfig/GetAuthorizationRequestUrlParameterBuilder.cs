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
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    ///     NOTE:  a few of the methods in AbstractAcquireTokenParameterBuilder (e.g. account) don't make sense here.
    ///     Do we want to create a further base that contains ALL of the common methods, and then have another one including
    ///     account, etc
    ///     that are only used for AcquireToken?
    /// </summary>
    public sealed class GetAuthorizationRequestUrlParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<GetAuthorizationRequestUrlParameterBuilder>
    {
        private GetAuthorizationRequestUrlParameters Parameters { get; } = new GetAuthorizationRequestUrlParameters();

        internal GetAuthorizationRequestUrlParameterBuilder(IConfidentialClientApplication confidentialClientApplication)
            : base(confidentialClientApplication)
        {
        }

        internal static GetAuthorizationRequestUrlParameterBuilder Create(
            IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes)
        {
            return new GetAuthorizationRequestUrlParameterBuilder(confidentialClientApplication).WithScopes(scopes);
        }

        /// <summary>
        /// Sets the redirect URI to add to the Authorization request URL
        /// </summary>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithRedirectUri(string redirectUri)
        {
            Parameters.RedirectUri = redirectUri;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="loginHint"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IConfidentialClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("This is a developer BUG.  This should never get executed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new Task<Uri> ExecuteAsync(CancellationToken cancellationToken)
        {
            // This method is marked "public new" because it only differs in return type from the base class
            // ExecuteAsync() and we need this one to return Uri and not AuthenticationResult.

            if (ConfidentialClientApplication is IConfidentialClientApplicationExecutor executor)
            {
                ValidateAndCalculateApiId();
                return executor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
            }

            throw new InvalidOperationException(
                "ConfidentialClientApplication implementation does not implement IConfidentialClientApplicationExecutor.");
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.None;
        }
    }
#endif
}
