// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for <see cref="IAccount"/>
    /// </summary>
    public static class AccountExtensions
    {
        /// <summary>        
        /// The same account can exist in its home tenant and also as a guest in multiple other tenants. 
        /// <c>TenantProfiles</c> maps a tenant ID to its associated tenant profile. <see cref="TenantProfile"/> is derived from the ID token for that tenant.
        /// </summary>
        /// <remarks>Only tenants for which a token was acquired will be available in <c>TenantProfiles</c> property</remarks>
        public static IDictionary<string, TenantProfile> GetTenantProfiles(this IAccount account)
        {
            if (account is Account accountObj)
            {
                return accountObj.TenantProfiles;
            }

            return null;
        }
    }
}
