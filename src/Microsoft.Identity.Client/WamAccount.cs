// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    internal class WamAccount : IAccount
    {
        public WamAccount(string wamAccountId, string userName, string environment)
        {
            HomeAccountId = new AccountId(wamAccountId);
            Username = userName;
            Environment = environment;
        }

        public string Username { get; }

        public string Environment { get; }

        public AccountId HomeAccountId { get; }
    }
}
