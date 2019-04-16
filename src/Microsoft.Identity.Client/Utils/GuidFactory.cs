// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Utils
{
    internal class GuidFactory : IGuidFactory
    {
        public Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
