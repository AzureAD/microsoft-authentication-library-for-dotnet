// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal interface ITelemetryDispatcher
    {
        void DispatchEvent(IMatsTelemetryBatch batch);
    }
}
