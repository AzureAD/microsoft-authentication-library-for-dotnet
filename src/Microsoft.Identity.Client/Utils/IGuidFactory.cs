// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Utils
{
    internal interface IGuidFactory
    {
        Guid NewGuid();
    }
}
