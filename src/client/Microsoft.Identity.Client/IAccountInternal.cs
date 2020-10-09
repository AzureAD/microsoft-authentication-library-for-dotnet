// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    internal interface IAccountInternal : IAccount
    {
        /// <summary>
        /// Dictionary of {client_id, internal_wam_account_id}.
        /// WamAccountId can be used to login into a broker account despite the broker not 
        /// returning the account via broker.GetAccounts(), which is a security measure.
        /// </summary>
        IDictionary<string, string> WamAccountIds { get; }
    }
}
