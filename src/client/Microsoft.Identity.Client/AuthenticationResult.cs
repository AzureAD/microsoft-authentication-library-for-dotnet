// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains the results of one token acquisition operation in <see cref="PublicClientApplication"/>
    /// or ConfidentialClientApplication. For details see https://aka.ms/msal-net-authenticationresult
    /// </summary>
    public partial class AuthenticationResult
    {
        private readonly IAuthenticationScheme _authenticationScheme;

        /// <summary>
        /// Constructor meant to help application developers test their apps. Allows mocking of authentication flows.
        /// App developers should <b>never</b> new-up <see cref="AuthenticationResult"/> in product code.
        /// </summary>
        /// <param name="accessToken">Access Token that can be used as a bearer token to access protected web APIs</param>
        /// <param name="account">Account information</param>
        /// <param name="expiresOn">Expiry date-time for the access token</param>
        /// <param name="extendedExpiresOn">See <see cref="ExtendedExpiresOn"/></param>
        /// <param name="idToken">ID token</param>
        /// <param name="isExtendedLifeTimeToken">See <see cref="IsExtendedLifeTimeToken"/></param>
        /// <param name="scopes">Granted scope values as returned by the service</param>
        /// <param name="tenantId">Identifier for the Azure AD tenant from which the token was acquired. Can be <c>null</c></param>
        /// <param name="uniqueId">Unique Id of the account. It can be null. When the <see cref="IdToken"/> is not <c>null</c>, this is its ID, that is its ObjectId claim, or if that claim is <c>null</c>, the Subject claim.</param>
        /// <param name="correlationId">The correlation id of the authentication request</param>
        /// <param name="tokenType">The token type, defaults to Bearer. Note: this property is experimental and may change in future versions of the library.</param>
        /// <param name="authenticationResultMetadata">Contains metadata related to the Authentication Result.</param>
        /// <param name="claimsPrincipal">Claims from the ID token</param>
        /// <param name="spaAuthCode">Auth Code returned by the Microsoft identity platform when you use AcquireTokenByAuthorizationCode.WithSpaAuthorizationCode(). This auth code is meant to be redeemed by the frontend code. See https://aka.ms/msal-net/spa-auth-code</param>
        /// <param name="additionalResponseParameters">Other properties from the token response.</param>
        public AuthenticationResult( // for backwards compat with 4.16-
            string accessToken,
            bool isExtendedLifeTimeToken,
            string uniqueId,
            DateTimeOffset expiresOn,
            DateTimeOffset extendedExpiresOn,
            string tenantId,
            IAccount account,
            string idToken,
            IEnumerable<string> scopes,
            Guid correlationId,
            string tokenType = "Bearer",
            AuthenticationResultMetadata authenticationResultMetadata = null, 
            ClaimsPrincipal claimsPrincipal = null,
            string spaAuthCode = null,
            IReadOnlyDictionary<string, string> additionalResponseParameters = null)
        {
            AccessToken = accessToken;
#pragma warning disable CS0618 // Type or member is obsolete
            IsExtendedLifeTimeToken = isExtendedLifeTimeToken;
            ExtendedExpiresOn = extendedExpiresOn;
#pragma warning restore CS0618 // Type or member is obsolete
            UniqueId = uniqueId;
            ExpiresOn = expiresOn;
            TenantId = tenantId;
            Account = account;
            IdToken = idToken;
            Scopes = scopes;
            CorrelationId = correlationId;
            TokenType = tokenType;
            AuthenticationResultMetadata = authenticationResultMetadata;
            ClaimsPrincipal = claimsPrincipal;
            SpaAuthCode = spaAuthCode;
            AdditionalResponseParameters = additionalResponseParameters;
        }

        /// <summary>
        /// Constructor meant to help application developers test their apps. Allows mocking of authentication flows.
        /// App developers should <b>never</b> new-up <see cref="AuthenticationResult"/> in product code.
        /// </summary>
        /// <param name="accessToken">Access Token that can be used as a bearer token to access protected web APIs</param>
        /// <param name="account">Account information</param>
        /// <param name="expiresOn">Expiry date-time for the access token</param>
        /// <param name="extendedExpiresOn">See <see cref="ExtendedExpiresOn"/></param>
        /// <param name="idToken">ID token</param>
        /// <param name="isExtendedLifeTimeToken">See <see cref="IsExtendedLifeTimeToken"/></param>
        /// <param name="scopes">Granted scope values as returned by the service</param>
        /// <param name="tenantId">Identifier for the Azure AD tenant from which the token was acquired. Can be <c>null</c></param>
        /// <param name="uniqueId">Unique Id of the account. It can be null. When the <see cref="IdToken"/> is not <c>null</c>, this is its ID, that is its ObjectId claim, or if that claim is <c>null</c>, the Subject claim.</param>
        /// <param name="correlationId">The correlation id of the authentication request</param>
        /// <param name="authenticationResultMetadata">Contains metadata related to the Authentication Result.</param>
        /// <param name="tokenType">The token type, defaults to Bearer. Note: this property is experimental and may change in future versions of the library.</param>
        /// <remarks>For backwards compatibility with MSAL 4.17-4.20 </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
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
          Guid correlationId,
          AuthenticationResultMetadata authenticationResultMetadata,
          string tokenType = "Bearer") :
            this(
                accessToken,
                isExtendedLifeTimeToken,
                uniqueId,
                expiresOn,
                extendedExpiresOn,
                tenantId,
                account,
                idToken,
                scopes,
                correlationId,
                tokenType,
                authenticationResultMetadata)
        {

        }

        internal AuthenticationResult(
            MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem, 
            IAuthenticationScheme authenticationScheme,
            Guid correlationID,
            TokenSource tokenSource, 
            ApiEvent apiEvent,
            Account account,
            string spaAuthCode, 
            IReadOnlyDictionary<string, string> additionalResponseParameters)
        {
            _authenticationScheme = authenticationScheme ?? throw new ArgumentNullException(nameof(authenticationScheme));
            
            string homeAccountId =
                msalAccessTokenCacheItem?.HomeAccountId ??
                msalIdTokenCacheItem?.HomeAccountId;
            string environment = msalAccessTokenCacheItem?.Environment ??
                msalIdTokenCacheItem?.Environment;

            ClaimsPrincipal = msalIdTokenCacheItem?.IdToken.ClaimsPrincipal;

            if (account != null)
            {
                Account = account;
            }
            else if (homeAccountId != null)
            {
                Account = new Account(
                    homeAccountId,
                    msalIdTokenCacheItem?.GetUsername(),
                    environment);
            }

            UniqueId = msalIdTokenCacheItem?.IdToken?.GetUniqueId();
            TenantId = msalIdTokenCacheItem?.IdToken?.TenantId;
            IdToken = msalIdTokenCacheItem?.Secret;
            SpaAuthCode = spaAuthCode;

            CorrelationId = correlationID;
            ApiEvent = apiEvent;
            AuthenticationResultMetadata = new AuthenticationResultMetadata(tokenSource);
            AdditionalResponseParameters = additionalResponseParameters;
            if (msalAccessTokenCacheItem != null)
            {
                AccessToken = authenticationScheme.FormatAccessToken(msalAccessTokenCacheItem);
                ExpiresOn = msalAccessTokenCacheItem.ExpiresOn;
                Scopes = msalAccessTokenCacheItem.ScopeSet;

#pragma warning disable CS0618 // Type or member is obsolete
                ExtendedExpiresOn = msalAccessTokenCacheItem.ExtendedExpiresOn;
                IsExtendedLifeTimeToken = msalAccessTokenCacheItem.IsExtendedLifeTimeToken;
#pragma warning restore CS0618 // Type or member is obsolete

                TokenType = msalAccessTokenCacheItem.TokenType;

                if (msalAccessTokenCacheItem.RefreshOn.HasValue)
                {
                    AuthenticationResultMetadata.RefreshOn = msalAccessTokenCacheItem.RefreshOn;
                }
            }
        }

        //Default constructor for testing
        internal AuthenticationResult() { }

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
        /// <remarks>This feature is not in use</remarks>
        [Obsolete("This feature has been deprecated", false)]
        [EditorBrowsable(EditorBrowsableState.Never)] // deprecated, this feature is not in use        
        public bool IsExtendedLifeTimeToken { get; }

        /// <summary>
        /// Gets the Unique Id of the account in this <see cref="TenantId" />
        /// It is set as the oid (ObjectId) claim, or if that claim is <c>null</c>, as the sub (Subject) claim which is guaranteed not-null.
        /// </summary>
        /// <remarks>
        /// The oid claim identifies a user in all apps - Microsoft Identity Providers issue ID tokens with this claim, although it can be null in rare cases.
        /// The sub claim is "a locally unique and never reassigned identifier within the Issuer for the End-User" as per https://openid.net/specs/openid-connect-core-1_0.html and it is a 
        /// mandatory claim with OIDC compliant issuers.
        /// Guest AAD accounts have different oid claim values in each tenant. Use <see cref="Account.HomeAccountId"/> to uniquely identify users across tenants.
        /// See https://docs.microsoft.com/azure/active-directory/develop/id-tokens#payload-claims
        /// </remarks>
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
        [EditorBrowsable(EditorBrowsableState.Never)] // deprecated, this feature is not in use
        [Obsolete("This feature has been deprecated", false)]
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
        public Guid CorrelationId { get; }

        /// <summary>
        /// Identifies the type of access token. By default tokens returned by Azure Active Directory are Bearer tokens.        
        /// <seealso cref="CreateAuthorizationHeader"/> for getting an HTTP authorization header from an AuthenticationResult.
        /// </summary>
        public string TokenType { get; }

        /// <summary>
        /// Gets the SPA Authorization Code, if it was requested using WithSpaAuthorizationCode method on the
        /// AcquireTokenByAuthorizationCode builder. See https://aka.ms/msal-net/spa-auth-code for details.
        /// </summary>
        public string SpaAuthCode { get; }

        /// <summary>
        /// Exposes additional response parameters returned by the token issuer (AAD).
        /// </summary>
        /// <remarks>
        /// Not all parameters are added here, only the ones that MSAL doesn't interpret itself and only scalars.
        /// Not supported on mobile frameworks (e.g. net6-android or net6-ios)
        /// </remarks>
        public IReadOnlyDictionary<string, string> AdditionalResponseParameters { get; }

        /// <summary>
        /// All the claims present in the ID token.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; }

        internal ApiEvent ApiEvent { get; }

        /// <summary>
        /// Contains metadata for the Authentication result.
        /// </summary>
        public AuthenticationResultMetadata AuthenticationResultMetadata { get; }

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
            return $"{_authenticationScheme?.AuthorizationHeaderPrefix ?? TokenType} {AccessToken}";
        }
    }
}
