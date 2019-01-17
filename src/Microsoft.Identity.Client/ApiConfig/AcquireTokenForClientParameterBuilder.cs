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

namespace Microsoft.Identity.Client.ApiConfig
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Builder for AcquireTokenForClient (used in client credential flows, in daemon applications).
    /// </summary>
    public sealed class AcquireTokenForClientParameterBuilder :
        AbstractCcaAcquireTokenParameterBuilder<AcquireTokenForClientParameterBuilder>
    {
        /// <inheritdoc />
        public AcquireTokenForClientParameterBuilder(IConfidentialClientApplication confidentialClientApplication)
            : base(confidentialClientApplication)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        internal static AcquireTokenForClientParameterBuilder Create(
            IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes)
        {
            return new AcquireTokenForClientParameterBuilder(confidentialClientApplication).WithScopes(scopes);
        }

        /// <summary>
        /// Specifies if the confidential client application should force refreshing the
        /// token from the application token cache. By default the token is taken from the
        /// the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, the Token request will ignore the access token in the application token cache
        /// and will attempt to acquire new access token using client credentials. The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS. 
        /// Sending the x5x enables application developers to achieve easy certificates roll-over in Azure AD: 
        /// this method will send the public certificate to Azure AD along with the token request, 
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy. 
        /// This saves the application admin from the need to explicitly manage the certificate rollover 
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>. 
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IConfidentialClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync((IAcquireTokenForClientParameters)Parameters, cancellationToken);
        }
    }
#endif
}