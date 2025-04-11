﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    internal class NoRetryPolicy : IRetryPolicy
    {
        public Task<bool> pauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            throw new NotImplementedException();
        }
    }
}
