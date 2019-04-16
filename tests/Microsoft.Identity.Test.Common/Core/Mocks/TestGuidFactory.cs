// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TestGuidFactory : IGuidFactory
    {
        public TestGuidFactory(Guid guid)
        {
            Guid = guid;
        }

        public Guid NewGuid()
        {
            return Guid;
        }

        public Guid Guid { get; set; }
    }
}
