// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http
{
    internal interface IRetryPolicy
    {
        int DelayInMilliseconds { get; }
        bool pauseForRetry(HttpResponse response, Exception exception, int retryCount);
    }
}
