// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ILegacyCacheIdentityFilter
    {
        // Per-operation snapshots using the lookup's aliases; null covers cross-environment removal.
        Func<string, bool> GetAccountFilter(IEnumerable<string> environments);
        Func<IAccount, string, bool> GetLegacyAccountFilter(IEnumerable<string> environments);
    }
}
