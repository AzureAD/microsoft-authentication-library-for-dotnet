// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Http.Retry;
using static Microsoft.Identity.Client.Internal.Constants;

namespace Microsoft.Identity.Test.Unit.Helpers
{
    internal class TestDefaultRetryPolicy : DefaultRetryPolicy
    {
        public TestDefaultRetryPolicy(RequestType requestType) : base(requestType) { }

        internal override Task DelayAsync(int milliseconds)
        {
            // No delay for tests
            return Task.CompletedTask;
        }
    }

    internal class TestImdsRetryPolicy : ImdsRetryPolicy
    {
        public TestImdsRetryPolicy() : base() { }

        internal override Task DelayAsync(int milliseconds)
        {
            // No delay for tests
            return Task.CompletedTask;
        }
    }
}
