// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
        public Account(string homeAccountId, string username, string environment)
        {
            Username = username;
            Environment = environment;
            HomeAccountId = AccountId.ParseFromString(homeAccountId);
        }

        public string Username { get; internal set; }

        public string Environment { get; internal set; }

        public AccountId HomeAccountId { get; internal set; }

        public override string ToString()
        {
            return string.Format(
            CultureInfo.CurrentCulture,
            "Account username: {0} environment {1} home account id: {2}",
            Username, Environment, HomeAccountId);
        }
    }
}
