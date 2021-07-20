// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains information of a single account. A user can be present in multiple directories and thus have multiple accounts.
    /// This information is used for token cache lookup and enforcing the user session on the STS authorize endpoint.
    /// </summary>
    internal sealed class Account : IAccount
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="homeAccountId">Home account id in "uid.utid" format; can be null, for example when migrating the ADAL v3 cache</param>
        /// <param name="username">UPN style , can be null</param>
        /// <param name="environment">Identity provider for this account, e.g. <c>login.microsoftonline.com</c></param>
        /// <param name="wamAccountIds">Map of (client_id, wam_account_id)</param>
        /// <param name="tenantProfiles">Map of (tenant_id, tenant_profile)</param>
        public Account(
            string homeAccountId, 
            string username, 
            string environment, 
            IDictionary<string, string> wamAccountIds = null, 
            IEnumerable<TenantProfile> tenantProfiles = null)
        {
            Username = username;
            Environment = environment;
            HomeAccountId = AccountId.ParseFromString(homeAccountId);
            WamAccountIds = wamAccountIds;
            TenantProfiles = tenantProfiles;
        }        

        public string Username { get; }

        public string Environment { get; }

        public AccountId HomeAccountId { get; }

        /// <summary>        
        /// The same account can exist in its home tenant and also as a guest in multiple other tenants. 
        /// A <see cref="TenantProfile"/> is derived from the ID token for that tenant.
        /// </summary>
        public IEnumerable<TenantProfile> TenantProfiles { get; }      

        internal IDictionary<string, string> WamAccountIds { get; }

        public override string ToString()
        {
            return string.Format(
            CultureInfo.CurrentCulture,
            "Account username: {0} environment {1} home account id: {2}",
            Username, Environment, HomeAccountId);
        }
    }
}
