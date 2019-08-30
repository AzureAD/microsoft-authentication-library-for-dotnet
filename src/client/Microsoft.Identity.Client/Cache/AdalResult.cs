// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Contains the results of one token acquisition operation.
    /// </summary>
    [DataContract]
    internal sealed class AdalResult
    {
        private const string Oauth2AuthorizationHeader = "Bearer ";

        /// <summary>
        /// Creates result returned from AcquireToken. Except in advanced scenarios related to token caching, you do not need to create any instance of AuthenticationResult.
        /// </summary>
        /// <param name="accessTokenType">Type of the Access Token returned</param>
        /// <param name="accessToken">The Access Token requested</param>
        /// <param name="expiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        internal AdalResult(string accessTokenType, string accessToken, DateTimeOffset expiresOn)
        {
            AccessTokenType = accessTokenType;
            AccessToken = accessToken;
            ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
            ExtendedExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Creates result returned from AcquireToken. Except in advanced scenarios related to token caching, you do not need to create any instance of AuthenticationResult.
        /// </summary>
        /// <param name="accessTokenType">Type of the Access Token returned</param>
        /// <param name="accessToken">The Access Token requested</param>
        /// <param name="expiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        /// <param name="extendedExpiresOn">The point in time in which the Access Token returned in the AccessToken property ceases to be valid</param>
        internal AdalResult(string accessTokenType, string accessToken, DateTimeOffset expiresOn,
            DateTimeOffset extendedExpiresOn)
        {
            AccessTokenType = accessTokenType;
            AccessToken = accessToken;
            ExpiresOn = DateTime.SpecifyKind(expiresOn.DateTime, DateTimeKind.Utc);
            ExtendedExpiresOn = DateTime.SpecifyKind(extendedExpiresOn.DateTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets the type of the Access Token returned.
        /// </summary>
        [DataMember]
        public string AccessTokenType { get; private set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        [DataMember]
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid in ADAL's extended LifeTime.
        /// This value is calculated based on current UTC time measured locally and the value ext_expiresIn received from the service.
        /// </summary>
        [DataMember]
        internal DateTimeOffset ExtendedExpiresOn { get; set; }

        /// <summary>
        /// Gives information to the developer whether token returned is during normal or extended lifetime.
        /// </summary>
        [DataMember]
        public bool ExtendedLifeTimeToken { get; internal set; }

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        public string TenantId { get; internal set; }

        /// <summary>
        /// Gets user information including user Id. Some elements in UserInfo might be null if not returned by the service.
        /// </summary>
        [DataMember]
        public AdalUserInfo UserInfo { get; internal set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        [DataMember]
        public string IdToken { get; internal set; }

        /// <summary>
        /// Gets the authority that has issued the token.
        /// </summary>
        public string Authority { get; internal set; }

        /// <summary>
        /// Creates authorization header from authentication result.
        /// </summary>
        /// <returns>Created authorization header</returns>
        public string CreateAuthorizationHeader()
        {
            return Oauth2AuthorizationHeader + AccessToken;
        }

        internal void UpdateTenantAndUserInfo(string tenantId, string idToken, AdalUserInfo userInfo)
        {
            TenantId = tenantId;
            IdToken = idToken;
            if (userInfo != null)
            {
                UserInfo = new AdalUserInfo(userInfo);
            }
        }
    }
}
