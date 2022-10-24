// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents an account in a specific tenant. The same account can exist in its home tenant and also as a guest in multiple other tenants.
    /// Access tokens and Id Tokens are tenant specific and this object provides high level information about all the ID tokens associated with the account.
    /// </summary>
    public class TenantProfile 
    {
        private readonly MsalIdTokenCacheItem _msalIdTokenCacheItem;

        internal TenantProfile(MsalIdTokenCacheItem msalIdTokenCacheItem) 
        {
            // DO NOT parse the IdToken in the ctor, this is an expensive operation!
            _msalIdTokenCacheItem = msalIdTokenCacheItem;
        }        

        /// <summary>
        /// The immutable identifier for an user account, in a specific tenant. 
        /// This ID uniquely identifies the user across applications - two different applications signing in the same user will receive the same value in the oid claim. 
        /// The user will have a different object ID in each tenant - they're considered different accounts, even though the user logs into each account with the same credentials. 
        /// </summary>
        public string Oid => _msalIdTokenCacheItem?.IdToken.ObjectId;

        /// <summary>
        /// Represents the tenant that the user is signing in to. 
        /// For work and school accounts, the GUID is the immutable tenant ID of the organization that the user is signing in to.
        /// For sign-ins to the personal Microsoft account tenant (services like Xbox, Teams for Life, or Outlook), the value is 9188040d-6c67-4c5b-b112-36a304b66dad. 
        /// </summary>
        public string TenantId => _msalIdTokenCacheItem?.IdToken.TenantId;

        /// <summary>
        /// All the claims present in the ID Token associated with this profile.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal => _msalIdTokenCacheItem?.IdToken.ClaimsPrincipal;

        /// <summary>
        /// Returns <c>true</c> if this profile is associated with the user's home tenant.
        /// </summary>
        public bool IsHomeTenant => string.Equals(
                AccountId.ParseFromString(_msalIdTokenCacheItem?.HomeAccountId).TenantId,
                _msalIdTokenCacheItem?.IdToken.TenantId,
                StringComparison.OrdinalIgnoreCase);

    }    
}
