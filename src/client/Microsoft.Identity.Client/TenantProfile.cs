// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents an account in a specific tenant. The same account can exist in its home tenant and also as a guest in multiple other tenants.
    /// Access tokens and Id Tokens are tenant specific and this object provides high level information about all the ID tokens associated with the account.
    /// </summary>
    public sealed class TenantProfile 
    {        
        /// <summary>
        /// Constructor 
        /// </summary>
        public TenantProfile(string oid, string tenantId, string idToken, bool isHomeTenant)
        {
            Oid = oid;
            TenantId = tenantId;            
            IsHomeTenant = isHomeTenant;
            ClaimsPrincipal = GetClaimsFromIdToken(idToken);
        }

        /// <summary>
        /// The immutable identifier for an user account, in a specific tenant. 
        /// This ID uniquely identifies the user across applications - two different applications signing in the same user will receive the same value in the oid claim. 
        /// The user will have a different object ID in each tenant - they're considered different accounts, even though the user logs into each account with the same credentials. 
        /// </summary>
        public string Oid { get; }

        /// <summary>
        /// Represents the tenant that the user is signing in to. 
        /// For work and school accounts, the GUID is the immutable tenant ID of the organization that the user is signing in to.
        /// For sign-ins to the personal Microsoft account tenant (services like Xbox, Teams for Life, or Outlook), the value is 9188040d-6c67-4c5b-b112-36a304b66dad. 
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// All the claims present in the ID Token.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; }

        /// <summary>
        /// Returns <c>true</c> if this profile is associated with the user's home tenant.
        /// </summary>
        public bool IsHomeTenant { get; }

        private ClaimsPrincipal GetClaimsFromIdToken(string idToken) // TODO: maybe we shold be passed the ClaimsPricipal?
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            string[] idTokenSegments = idToken.Split(new[] { '.' });

            if (idTokenSegments.Length < 3)
            {
                throw new MsalClientException(
                    MsalError.InvalidJwtError,
                    MsalErrorMessage.IDTokenInvalidJwtFormat);
            }

            try
            {
                IDictionary<string, string> idTokenClaims = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    Base64UrlHelpers.DecodeToString(idTokenSegments[1]));

                IList<Claim> claims = new List<Claim>();
                foreach (KeyValuePair<string, string> claim in idTokenClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }

                return new ClaimsPrincipal(new ClaimsIdentity(claims));
            }
            catch (JsonException exc)
            {
                throw new MsalClientException(
                    MsalError.JsonParseError,
                    MsalErrorMessage.FailedToParseIDToken,
                    exc);
            }
        }
    }

    
}
