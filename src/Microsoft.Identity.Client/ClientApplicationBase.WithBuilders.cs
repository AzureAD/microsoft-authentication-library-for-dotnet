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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    public abstract partial class ClientApplicationBase : IClientApplicationBaseExecutor
    {
        internal ClientApplicationBase(ApplicationConfiguration config)
        {
            ServiceBundle = Core.ServiceBundle.Create(config);

            if (config.UserTokenLegacyCachePersistenceForTest != null)
            {
                UserTokenCacheInternal = new TokenCache(ServiceBundle, config.UserTokenLegacyCachePersistenceForTest);
            }
            else
            {
                UserTokenCacheInternal = new TokenCache(ServiceBundle);
            }
        }

        internal void LogVersionInfo()
        {
            CreateRequestContext().Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}'",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    AssemblyUtils.GetAssemblyFileVersionAttribute(),
                    AssemblyUtils.GetAssemblyInformationalVersion()));
        }

        async Task<AuthenticationResult> IClientApplicationBaseExecutor.ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            LogVersionInfo();

            var customAuthority = commonParameters.AuthorityOverride == null
                                      ? GetAuthority(silentParameters.Account)
                                      : Instance.Authority.CreateAuthorityWithOverride(ServiceBundle, commonParameters.AuthorityOverride);

            var requestParameters = CreateRequestParameters(commonParameters, UserTokenCacheInternal, customAuthority);

            requestParameters.Account = silentParameters.Account;
            requestParameters.LoginHint = silentParameters.LoginHint;

            var handler = new SilentRequest(ServiceBundle, requestParameters, silentParameters);
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IClientApplicationBaseExecutor.ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters refreshTokenParameters,
            CancellationToken cancellationToken)
        {
            LogVersionInfo();

            var requestContext = CreateRequestContext();
            if (commonParameters.Scopes == null || !commonParameters.Scopes.Any())
            {
                commonParameters.Scopes = new SortedSet<string>
                {
                    ClientId + "/.default"
                };
                requestContext.Logger.Info(LogMessages.NoScopesProvidedForRefreshTokenRequest);
            }

            var requestParameters = CreateRequestParameters(commonParameters, UserTokenCacheInternal);
            requestParameters.IsRefreshTokenRequest = true;

            requestContext.Logger.Info(LogMessages.UsingXScopesForRefreshTokenRequest(commonParameters.Scopes.Count()));

            var handler = new ByRefreshTokenRequest(ServiceBundle, requestParameters, refreshTokenParameters);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V3 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache, 
        /// with advanced parameters controlling the network call. See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. This parameter is optional.
        /// If nothing is passed and no Account or LoginHint are provided then if one and only
        /// one account is in the cache, that account is used.  Otherwise, an exception will be thrown.  <see cref="IAccount"/></param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters</returns>
        /// <exception cref="MsalUiRequiredException">will be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache,a or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or the user needs to perform two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See also the additional parameters that you can set chain:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/> or one of its
        /// overrides to request a token for a different authority than the one set at the application construction
        /// <see cref="AcquireTokenSilentParameterBuilder.WithForceRefresh(bool)"/> to bypass the user token cache and
        /// force refreshing the token, as well as
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to
        /// specify extra query parameters
        /// 
        /// You can also use null for <paramref name="account"/> and then use one of the following:
        /// <see cref="AcquireTokenSilentParameterBuilder.WithAccount(IAccount)"/> or 
        /// <see cref="AcquireTokenSilentParameterBuilder.WithLoginHint(string)"/> to specify the account in the
        /// case where your application manages several accounts.
        /// </remarks>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account = null)
        {
            return AcquireTokenSilentParameterBuilder.Create(this, scopes, account);
        }
    }
}
