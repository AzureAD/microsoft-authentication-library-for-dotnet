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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Base class for builders of token requests, which attempt to acquire a token
    /// based on the provided parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AcquireTokenParameters Parameters { get; } = new AcquireTokenParameters();

        /// <summary>
        /// Executes the Token request asynchronously, with a possibility of cancelling the
        /// asynchronous method.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token. See <see cref="CancellationToken"/> </param>
        /// <returns>Authentication result containing a token for the requested scopes and parameters
        /// set in the builder</returns>
        public abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Specifies which scopes to request
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>The builder to chain the .With methods</returns>
        protected T WithScopes(IEnumerable<string> scopes)
        {
            Parameters.Scopes = scopes;
            return (T)this;
        }

        /// <summary>
        /// Sets the <paramref name="loginHint"/>, in order to avoid select account
        /// dialogs in the case the user is signed-in with several identities. This method is mutually exclusive
        /// with <see cref="WithAccount(IAccount)"/>. If both are used, an exception will be thrown
        /// </summary>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;
            return (T)this;
        }

        /// <summary>
        /// Sets the account for which the token will be retrieved. This method is mutually exclusive
        /// with <see cref="WithLoginHint(string)"/>. If both are used, an exception will be thrown
        /// </summary>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return (T)this;
        }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority
        /// as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithExtraQueryParameters(Dictionary<string, string> extraQueryParameters)
        {
            Parameters.ExtraQueryParameters = extraQueryParameters ?? new Dictionary<string, string>();
            return (T)this;
        }

        // This exists for back compat with old-style API.  Once we deprecate it, we can remove this.
        internal T WithExtraQueryParameters(string extraQueryParameters)
        {
            return WithExtraQueryParameters(CoreHelpers.ParseKeyValueList(extraQueryParameters, '&', true, null));
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront,
        /// in addition to the scopes for the protected Web API for which you want to acquire a security token.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return (T)this;
        }

        /// <summary>
        /// Specific authority for which the token is requested. Passing a different value than configured
        /// at the application constructor narrows down the selection to a specific tenant.
        /// This does not change the configured value in the application. This is specific
        /// to applications managing several accounts (like a mail client with several mailboxes)
        /// </summary>
        /// <param name="authorityUri">Uri for the authority</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthorityOverride(string authorityUri)
        {
            Parameters.AuthorityOverride = authorityUri;
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Validate()
        {
            if (Parameters.Scopes == null)
            {
                throw new ArgumentException("Scopes cannot be null", nameof(Parameters.Scopes));
            }
        }
    }
}