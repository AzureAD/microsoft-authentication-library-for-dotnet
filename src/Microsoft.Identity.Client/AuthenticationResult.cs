// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;

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
        /// App developers should <b>never</b> new-up <see cref="AuthenticationResult"/> in product code.
        /// </summary>
        /// <param name="accessToken">Access Token that can be used as a bearer token to access protected web APIs</param>
        /// <param name="account">Account information</param>
        /// <param name="expiresOn">Expiracy date-time for the access token</param>
        /// <param name="extendedExpiresOn">See <see cref="ExtendedExpiresOn"/></param>
        /// <param name="idToken">ID token</param>
        /// <param name="isExtendedLifeTimeToken">See <see cref="IsExtendedLifeTimeToken"/></param>
        /// <param name="scopes">granted scope values as returned by the service</param>
        /// <param name="tenantId">identifier for the Azure AD tenant from which the token was acquired. Can be <c>null</c></param>
        /// <param name="uniqueId">Unique Id of the account. It can be null. When the <see cref="IdToken"/> is not <c>null</c>, this is its ID, that
        /// <param name="correlationID">The correlation id of the authentication request</param>
        /// is its ObjectId claim, or if that claim is <c>null</c>, the Subject claim.</param>
        public AuthenticationResult(
            string accessToken,
            bool isExtendedLifeTimeToken,
            string uniqueId,
            DateTimeOffset expiresOn,
            DateTimeOffset extendedExpiresOn,
            string tenantId,
            IAccount account,
            string idToken,
            IEnumerable<string> scopes,
            string correlationID)
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
            CorrelationID = correlationID;
        }

        internal AuthenticationResult()
        {
        }

        internal AuthenticationResult(MsalAccessTokenCacheItem msalAccessTokenCacheItem, MsalIdTokenCacheItem msalIdTokenCacheItem, string correlationID)
        {
            if (msalAccessTokenCacheItem.HomeAccountId != null)
            {
                string username = msalAccessTokenCacheItem.IsAdfs ? msalIdTokenCacheItem?.IdToken.Upn : msalIdTokenCacheItem?.IdToken?.PreferredUsername;
                Account = new Account(
                    msalAccessTokenCacheItem.HomeAccountId,
                    username,
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
            CorrelationID = correlationID;
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
        /// as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> or
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
        /// Gets the correlation id used for the request.
        /// </summary>
        public string CorrelationID { get; }

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
