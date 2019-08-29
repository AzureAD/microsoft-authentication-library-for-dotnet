// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class ReadAccountResponse
    {
        public ReadAccountResponse(Microsoft.Identity.Client.CacheV2.Schema.Account account, OperationStatus status)
        {
            Account = account;
            Status = status;
        }

        public Microsoft.Identity.Client.CacheV2.Schema.Account Account { get; }
        public OperationStatus Status { get; }
    }
}
