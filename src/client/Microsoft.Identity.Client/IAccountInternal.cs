// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    internal interface IAccountInternal : IAccount
    {
        IDictionary<string, string> WamAccountIds { get; }
    }
}
