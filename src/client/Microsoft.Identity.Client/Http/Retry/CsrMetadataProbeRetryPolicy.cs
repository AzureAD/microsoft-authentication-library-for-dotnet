// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class CsrMetadataProbeRetryPolicy : ImdsRetryPolicy
    {
        protected override bool ShouldRetry(HttpResponse response, Exception exception)
        {
            return HttpRetryConditions.CsrMetadataProbe(response, exception);
        }
    }
}
