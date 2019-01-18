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

            CreateRequestContext().Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
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
            var customAuthority = string.IsNullOrWhiteSpace(commonParameters.AuthorityOverride)
                                      ? GetAuthority(silentParameters.Account)
                                      : Instance.Authority.CreateAuthority(ServiceBundle, commonParameters.AuthorityOverride);

            var requestParameters = CreateRequestParameters(commonParameters, UserTokenCacheInternal, customAuthority);

            requestParameters.Account = silentParameters.Account;
            requestParameters.LoginHint = silentParameters.LoginHint;

            var handler = new SilentRequest(ServiceBundle, requestParameters, silentParameters);
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IClientApplicationBaseExecutor.ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters byRefreshTokenParameters,
            CancellationToken cancellationToken)
        {
            var requestParameters = CreateRequestParameters(commonParameters, UserTokenCacheInternal);
            requestParameters.IsRefreshTokenRequest = true;

            var requestContext = CreateRequestContext();
            if (commonParameters.Scopes == null || !commonParameters.Scopes.Any())
            {
                commonParameters.Scopes = new SortedSet<string>
                {
                    ClientId + "/.default"
                };
                requestContext.Logger.Info(LogMessages.NoScopesProvidedForRefreshTokenRequest);
            }

            requestContext.Logger.Info(LogMessages.UsingXScopesForRefreshTokenRequest(commonParameters.Scopes.Count()));

            var handler = new ByRefreshTokenRequest(ServiceBundle, requestParameters, byRefreshTokenParameters);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account)
        {
            return AcquireTokenSilentParameterBuilder.Create(this, scopes, account);
        }
    }
}