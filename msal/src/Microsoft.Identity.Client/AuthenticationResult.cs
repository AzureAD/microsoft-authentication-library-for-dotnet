//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Contains the results of one token acquisition operation in <see cref="PublicClientApplication"/>
    /// or <see cref="T:ConfidentialClientApplication"/>. For details see https://aka.ms/msal-net-authenticationresult
    /// </summary> 
    public partial class AuthenticationResult
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";
        private readonly MsalAccessTokenCacheItem _msalAccessTokenCacheItem;
        private readonly MsalIdTokenCacheItem _msalIdTokenCacheItem;


        internal AuthenticationResult()
        {
        }

        internal AuthenticationResult(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            _msalAccessTokenCacheItem = msalAccessTokenCacheItem;
            _msalIdTokenCacheItem = msalIdTokenCacheItem;
            if (_msalAccessTokenCacheItem.HomeAccountId != null)
            {
                Account = new Account(AccountId.FromClientInfo(_msalAccessTokenCacheItem.ClientInfo),
                    _msalIdTokenCacheItem?.IdToken?.PreferredUsername, _msalAccessTokenCacheItem.Environment);
            }
        }

        /// <summary>
        /// Gets the Access Token to use as a bearer token to access the protected web API
        /// </summary>
        public virtual string AccessToken => _msalAccessTokenCacheItem.Secret;

        /// <summary>
        /// Gets the Unique Id of the account. It can be null. When the <see cref="IdToken"/> is not <c>null</c>, this is its ID, that
        /// is its ObjectId claim, or if that claim is <c>null</c>, the Subject claim.
        /// </summary>
        public virtual string UniqueId => _msalIdTokenCacheItem?.IdToken?.GetUniqueId();

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the <see cref="AccessToken"/> property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the
        /// service.
        /// </summary>
        public virtual DateTimeOffset ExpiresOn => _msalAccessTokenCacheItem.ExpiresOn;

        /// <summary>
        /// Gets an identifier for the Azure AD tenant from which the token was acquired. This property will be null if tenant information is
        /// not returned by the service.
        /// </summary>
        public virtual string TenantId => _msalIdTokenCacheItem?.IdToken?.TenantId;

        /// <summary>
        /// Gets the account information. Some elements in <see cref="IAccount"/> might be null if not returned by the
        /// service. The account can be passed back in some API overloads to identify which account should be used such 
        /// as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/> or
        /// <see cref="IClientApplicationBase.RemoveAsync(IAccount)"/> for instance
        /// </summary>
        public virtual IAccount Account { get; internal set; }

        /// <summary>
        /// Gets the  Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public virtual string IdToken => _msalIdTokenCacheItem.Secret;

        /// <summary>
        /// Gets the granted scope values returned by the service.
        /// </summary>
        public virtual IEnumerable<string> Scopes => _msalAccessTokenCacheItem.ScopeSet;

        /// <summary>
        /// Creates the content for an HTTP authorization header from this authentication result, so
        /// that you can call a protected API
        /// </summary>
        /// <returns>Created authorization header of the form "Bearer {AccessToken}"</returns>
        /// <example>
        /// Here is how you can call a protected API from this authentication result (in the <c>result</c>
        /// variable):
        /// <code>
        /// HttpClient client = new HttpClient();
        /// client.DefaultRequestHeaders.Add("Authorization", result.CreateAuthorizationHeader());
        /// HttpResponseMessage r = await client.GetAsync(urlOfTheProtectedApi);
        /// </code>
        /// </example>
        public virtual string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + AccessToken;
        }
    }
}