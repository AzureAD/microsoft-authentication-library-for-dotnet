// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    public class TestTimeService : ITimeService
    {
        private DateTime _utcNow;

        public TestTimeService()
        {
            _utcNow = DateTime.UtcNow;
        }

        public TestTimeService(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTime GetUtcNow()
        {
            return _utcNow;
        }

        public void MoveToFuture(TimeSpan span)
        {
            _utcNow = _utcNow + span;
        }

        public void MoveToPast(TimeSpan span)
        {
            _utcNow = _utcNow - span;
        }
    }
}
