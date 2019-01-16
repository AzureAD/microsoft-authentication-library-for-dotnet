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
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// </summary>
    public sealed class AcquireTokenOnBehalfOfParameterBuilder :
        AbstractCcaAcquireTokenParameterBuilder<AcquireTokenOnBehalfOfParameterBuilder>
    {
        /// <inheritdoc />
        public AcquireTokenOnBehalfOfParameterBuilder(IConfidentialClientApplication confidentialClientApplication)
            : base(confidentialClientApplication)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="confidentialClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="userAssertion"></param>
        /// <returns></returns>
        internal static AcquireTokenOnBehalfOfParameterBuilder Create(
            IConfidentialClientApplication confidentialClientApplication,
            IEnumerable<string> scopes, 
            UserAssertion userAssertion)
        {
            return new AcquireTokenOnBehalfOfParameterBuilder(confidentialClientApplication)
                   .WithScopes(scopes)
                   .WithUserAssertion(userAssertion);
        }

        private AcquireTokenOnBehalfOfParameterBuilder WithUserAssertion(UserAssertion userAssertion)
        {
            Parameters.UserAssertion = userAssertion;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="withSendX5C"></param>
        /// <returns></returns>
        public AcquireTokenOnBehalfOfParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IConfidentialClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync((IAcquireTokenOnBehalfOfParameters)Parameters, cancellationToken);
        }
    }
#endif
}