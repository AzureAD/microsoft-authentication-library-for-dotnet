// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class ReadAccountsResponse
    {
        public ReadAccountsResponse(IEnumerable<Account> accounts, OperationStatus status)
        {
            Accounts = accounts;
            Status = status;
        }

        public IEnumerable<Account> Accounts { get; }
        public OperationStatus Status { get; }
    }
}
