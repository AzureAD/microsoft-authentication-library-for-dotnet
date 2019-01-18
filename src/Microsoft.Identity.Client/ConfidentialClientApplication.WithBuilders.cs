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
    public partial class ConfidentialClientApplication : IConfidentialClientApplicationExecutor
    {
        internal ConfidentialClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();
            AppTokenCacheInternal = new TokenCache(ServiceBundle);
        }

        /// <summary>
        /// Acquires a security token from the authority configured in the app using the authorization code 
        /// previously received from the STS. 
        /// It uses the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in Web Apps (for instance ASP.NET / ASP.NET Core Web apps) which sign-in users,
        /// and can request an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up 
        /// using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can set optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthorityOverride(string)"/>, 
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>,
        /// </remarks>
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
        /// Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overriden in the application.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForClientParameterBuilder.WithForceRefresh(bool)"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// </remarks>
        public AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(this, scopes);
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from the authority configured in the application, 
        /// in order to access another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. 
        /// (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// </remarks>
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            return AcquireTokenOnBehalfOfParameterBuilder.Create(this, scopes, userAssertion);
        }

        /// <summary>
        /// Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application.
        /// This override enables you to specify a login hint and extra query parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request to get the
        /// URL of the STS authorization endpoint parametrized with the parameters</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="GetAuthorizationRequestUrlParameterBuilder.WithRedirectUri(string)"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithLoginHint(string)"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraScopesToConsent(IEnumerable{string})"/>
        /// </remarks>
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
                ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope); 
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenForClientParameters clientParameters,
            CancellationToken cancellationToken)
        {
            ApiEvent.ApiIds apiId = ApiEvent.ApiIds.AcquireTokenForClientWithScope;
            if (clientParameters.ForceRefresh)
            {
                apiId = ApiEvent.ApiIds.AcquireTokenForClientWithScopeRefresh;
            }

            var requestParams = CreateRequestParameters(clientParameters, AppTokenCacheInternal);
            requestParams.IsClientCredentialRequest = true;
            requestParams.SendCertificate = clientParameters.SendX5C;
            var handler = new ClientCredentialRequest(
                ServiceBundle,
                requestParams,
                apiId,
                clientParameters.ForceRefresh);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IConfidentialClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenOnBehalfOfParameters onBehalfOfParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(onBehalfOfParameters, UserTokenCacheInternal);
            requestParams.UserAssertion = onBehalfOfParameters.UserAssertion;
            requestParams.SendCertificate = onBehalfOfParameters.SendX5C;
            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUser);

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
                Prompt.SelectAccount,
                null);

            // todo: need to pass through cancellation token here
            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }
    }
#endif
}