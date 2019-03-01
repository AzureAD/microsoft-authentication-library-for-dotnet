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

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Base class for parameter builders common to public client application and confidential
    /// client application token acquisition operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractClientAppBaseAcquireTokenParameterBuilder<T> : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        /// <summary>
        /// Constructor of base class for parameter builders common to public client application and confidential
        /// client application token acquisition operations
        /// </summary>
        /// <param name="clientApplicationBase"></param>
        protected AbstractClientAppBaseAcquireTokenParameterBuilder(IClientApplicationBase clientApplicationBase)
        {
            ClientApplicationBase = clientApplicationBase;
        }

        /// <summary>
        /// Affected application
        /// </summary>
        protected IClientApplicationBase ClientApplicationBase { get; }

        internal abstract Task<AuthenticationResult> ExecuteAsync(
            IClientApplicationBaseExecutor executor,
            CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (ClientApplicationBase is IClientApplicationBaseExecutor executor)
            {
                ValidateAndCalculateApiId();
                return ExecuteAsync(executor, cancellationToken);
            }

            throw new InvalidOperationException(CoreErrorMessages.ClientApplicationBaseExecutorNotImplemented);
        }
    }
}