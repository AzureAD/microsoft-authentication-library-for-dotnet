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
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Helpers;

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

        /// <summary>
        /// Constructor meant to help application developers test their apps. Allows mocking of authentication flows. 
        /// App developers should never new-up <see cref="AuthenticationResult"/> in product code.
        /// </summary>
        public AuthenticationResult(
            string accessToken, 
            bool isExtendedLifeTimeToken, 
            string uniqueId, 
            DateTimeOffset expiresOn, 
            DateTimeOffset extendedExpiresOn, 
            string tenantId, 
            IAccount account, 
            string idToken, 
            IEnumerable<string> scopes)
        {
            AccessToken = accessToken;
            IsExtendedLifeTimeToken = isExtendedLifeTimeToken;
            UniqueId = uniqueId;
            ExpiresOn = expiresOn;
            ExtendedExpiresOn = extendedExpiresOn;
            TenantId = tenantId;
            Account = account;
            IdToken = idToken;
            Scopes = scopes;
        }

        internal AuthenticationResult()
        {
        }

        internal AuthenticationResult(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            if (msalAccessTokenCacheItem.HomeAccountId != null)
            {
                this.Account = new Account(
                    msalAccessTokenCacheItem.HomeAccountId,
                    msalIdTokenCacheItem?.IdToken?.PreferredUsername,
                    msalAccessTokenCacheItem.Environment);
            }

            AccessToken = msalAccessTokenCacheItem.Secret;
            UniqueId = msalIdTokenCacheItem?.IdToken?.GetUniqueId();
            ExpiresOn = msalAccessTokenCacheItem.ExpiresOn;
            ExtendedExpiresOn = msalAccessTokenCacheItem.ExtendedExpiresOn;
            TenantId = msalIdTokenCacheItem?.IdToken?.TenantId;
            IdToken = msalIdTokenCacheItem?.Secret;
            Scopes = msalAccessTokenCacheItem.ScopeSet;
            IsExtendedLifeTimeToken = msalAccessTokenCacheItem.IsExtendedLifeTimeToken;
        }

        /// <summary>
        /// Access Token that can be used as a bearer token to access protected web APIs
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// In case when Azure AD has an outage, to be more resilient, it can return tokens with
        /// an expiration time, and also with an extended expiration time.
        /// The tokens are then automatically refreshed by MSAL when the time is more than the
        /// expiration time, except when ExtendedLifeTimeEnabled is true and the time is less
        /// than the extended expiration time. This goes in pair with Web APIs middleware which,
        /// when this extended life time is enabled, can accept slightly expired tokens.
        /// Client applications accept extended life time tokens only if
        /// the ExtendedLifeTimeEnabled Boolean is set to true on ClientApplicationBase.
        /// </summary>
        public bool IsExtendedLifeTimeToken { get; }

        /// <summary>
        /// Gets the Unique Id of the account. It can be null. When the <see cref="IdToken"/> is not <c>null</c>, this is its ID, that
        /// is its ObjectId claim, or if that claim is <c>null</c>, the Subject claim.
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the <see cref="AccessToken"/> property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the
        /// service.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid in MSAL's extended LifeTime.
        /// This value is calculated based on current UTC time measured locally and the value ext_expiresIn received from the service.
        /// </summary>
        public DateTimeOffset ExtendedExpiresOn { get; }

        /// <summary>
        /// Gets an identifier for the Azure AD tenant from which the token was acquired. This property will be null if tenant information is
        /// not returned by the service.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the account information. Some elements in <see cref="IAccount"/> might be null if not returned by the
        /// service. The account can be passed back in some API overloads to identify which account should be used such 
        /// as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/> or
        /// <see cref="IClientApplicationBase.RemoveAsync(IAccount)"/> for instance
        /// </summary>
        public IAccount Account { get; }

        /// <summary>
        /// Gets the  Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string IdToken { get; }

        /// <summary>
        /// Gets the granted scope values returned by the service.
        /// </summary>
        public IEnumerable<string> Scopes { get; }

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
        public string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + AccessToken;
        }
    }
}