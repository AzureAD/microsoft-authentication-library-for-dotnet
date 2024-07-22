// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains information about a single account. A user can be present in multiple directories and thus have multiple accounts.
    /// This information is used for token cache lookup and enforcing the user session on the STS authorize endpoint.
    /// </summary>
    internal sealed class Account : IAccount
    {
        /// <summary>
        /// Constructor for the account instance.
        /// </summary>
        /// <param name="homeAccountId">Home account ID in "uid.utid" format; can be null, for example when migrating the ADAL v3 cache.</param>
        /// <param name="username"><see href="https://learn.microsoft.com/windows/win32/secauthn/user-name-formats#user-principal-name">UPN-style</see>, can be null</param>
        /// <param name="environment">Identity provider for the account, e.g., <c>login.microsoftonline.com</c>.</param>
        /// <param name="accountsource">The initial flow that established the account. e.g., device code flow.</param>
        /// <param name="wamAccountIds">Map of (<c>client_id</c>, <c>wam_account_id</c>)</param>
        /// <param name="tenantProfiles">Map of (<c>tenant_id</c>, <c>tenant_profile</c>)</param>
        public Account(
            string homeAccountId, 
            string username, 
            string environment,
            string accountsource = null,
            IDictionary<string, string> wamAccountIds = null, 
            IEnumerable<TenantProfile> tenantProfiles = null)
        {
            Username = username;
            Environment = environment;
            AccountSource = accountsource;
            HomeAccountId = AccountId.ParseFromString(homeAccountId);
            WamAccountIds = wamAccountIds;
            TenantProfiles = tenantProfiles;
        }        
        
        /// <summary>
        /// Gets the username associated with the account. For example, <c>account@example.com</c>.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the environment associated with the account. For example, <c>login.microsoftonline.com</c>.
        /// </summary>
        public string Environment { get; }

        /// <summary>
        /// Gets the source of the account. For example, device code flow, broker etc.
        /// </summary>
        public string AccountSource { get; }

        /// <summary>
        /// Gets additional account identifiers, such as object ID, tenant ID, and the unique identifier.
        /// </summary>
        public AccountId HomeAccountId { get; }

        /// <summary>
        /// Gets the list of tenant profiles.
        /// </summary>
        /// <remarks>
        /// The same account can exist in its home tenant and also as a guest in multiple other tenants. 
        /// A <see cref="TenantProfile"/> is derived from the ID token for that tenant.
        /// </remarks>
        public IEnumerable<TenantProfile> TenantProfiles { get; }      

        /// <summary>
        /// Gets a dictionary representing the mapping between the requesting client ID and the unique account ID.
        /// </summary>
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
