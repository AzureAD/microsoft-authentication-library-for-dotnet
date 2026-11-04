// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ILegacyCacheIdentityFilter
    {
        // A per-operation snapshot; null environments covers cross-environment ADAL removal.
        Func<IAccount, bool> GetLegacyAccountFilter(IEnumerable<string> environments);
    }
}
