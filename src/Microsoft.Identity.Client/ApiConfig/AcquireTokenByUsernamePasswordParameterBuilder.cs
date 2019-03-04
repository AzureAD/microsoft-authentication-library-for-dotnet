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
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Parameter builder for the <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(IEnumerable{string}, string, SecureString)"/>
    /// operation. See https://aka.ms/msal-net-up
    /// </summary>
    public sealed class AcquireTokenByUsernamePasswordParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenByUsernamePasswordParameterBuilder>
    {
        private AcquireTokenByUsernamePasswordParameters Parameters { get; } = new AcquireTokenByUsernamePasswordParameters();

        /// <inheritdoc />
        internal AcquireTokenByUsernamePasswordParameterBuilder(IPublicClientApplication publicClientApplication)
            : base(publicClientApplication)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        internal static AcquireTokenByUsernamePasswordParameterBuilder Create(
            IPublicClientApplication publicClientApplication,
            IEnumerable<string> scopes,
            string username,
            SecureString password)
        {
            return new AcquireTokenByUsernamePasswordParameterBuilder(publicClientApplication)
                   .WithScopes(scopes).WithUsername(username).WithPassword(password);
        }

        private AcquireTokenByUsernamePasswordParameterBuilder WithUsername(string username)
        {
            Parameters.Username = username;
            return this;
        }

        private AcquireTokenByUsernamePasswordParameterBuilder WithPassword(SecureString password)
        {
            Parameters.Password = password;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IPublicClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenWithScopeUser;
        }
    }
}