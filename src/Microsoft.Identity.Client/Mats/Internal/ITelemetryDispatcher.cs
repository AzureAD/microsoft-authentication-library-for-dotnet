// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal interface ITelemetryDispatcher
    {
        void DispatchEvent(IMatsTelemetryBatch batch);
    }
}
