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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public sealed class AcquireTokenSilentParameterBuilder :
        AbstractClientAppBaseAcquireTokenParameterBuilder<AcquireTokenSilentParameterBuilder>
    {
        private AcquireTokenSilentParameters Parameters { get; } = new AcquireTokenSilentParameters();

        /// <inheritdoc />
        internal AcquireTokenSilentParameterBuilder(IClientApplicationBase clientApplicationBase)
            : base(clientApplicationBase)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="clientApplicationBase"></param>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        internal static AcquireTokenSilentParameterBuilder Create(
            IClientApplicationBase clientApplicationBase,
            IEnumerable<string> scopes,
            IAccount account)
        {
            return new AcquireTokenSilentParameterBuilder(clientApplicationBase).WithScopes(scopes).WithAccount(account);
        }

      
        private AcquireTokenSilentParameterBuilder WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return this;
        }
     

        /// <summary>
        /// Specifies if the client application should force refreshing the
        /// token from the user token cache. By default the token is taken from the
        /// the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the user token cache
        /// and attempt to acquire new access token using the refresh token for the account
        /// if one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>Avoid un-necessarily setting <paramref name="forceRefresh"/> to <c>true</c> true in order to
        /// avoid negatively affecting the performance of your application</remarks>
        public AcquireTokenSilentParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IClientApplicationBaseExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return CommonParameters.AuthorityOverride == null
                ? ApiEvent.ApiIds.AcquireTokenSilentWithoutAuthority
                : ApiEvent.ApiIds.AcquireTokenSilentWithAuthority;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (Parameters.Account == null)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.UserNullError, MsalErrorMessage.MsalUiRequiredMessage);
            }
        }
    }
}