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

namespace Microsoft.Identity.Client.CallConfig
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AcquireTokenParameters Parameters { get; } = new AcquireTokenParameters();

        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public T WithScopes(IEnumerable<string> scopes)
        {
            Parameters.Scopes = scopes;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="loginHint"></param>
        /// <returns></returns>
        public T WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public T WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return (T)this;
        }

        /// <summary>
        ///     todo: make the param here a dictionary instead of a raw string?
        /// </summary>
        /// <param name="extraQueryParameters"></param>
        /// <returns></returns>
        public T WithExtraQueryParameters(string extraQueryParameters)
        {
            Parameters.ExtraQueryParameters = extraQueryParameters;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent"></param>
        /// <returns></returns>
        public T WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return (T)this;
        }

        /// <summary>
        ///     TODO: replicate the options here that we have in ApplicationBuilder for an AuthorityInfo class?
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <returns></returns>
        public T WithAuthorityOverride(string authorityUri)
        {
            Parameters.AuthorityOverride = authorityUri;
            return (T)this;
        }
    }
}