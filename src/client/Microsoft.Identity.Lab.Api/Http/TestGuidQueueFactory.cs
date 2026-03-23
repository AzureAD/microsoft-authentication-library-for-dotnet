// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TestGuidQueueFactory : IGuidFactory
    {
        private readonly List<Guid> _guids;

        public TestGuidQueueFactory(IEnumerable<Guid> guids)
        {
            _guids = guids.ToList();
        }

        public Guid NewGuid()
        {
            Guid guid = _guids[0];
            _guids.RemoveAt(0);
            return guid;
        }
    }
}
