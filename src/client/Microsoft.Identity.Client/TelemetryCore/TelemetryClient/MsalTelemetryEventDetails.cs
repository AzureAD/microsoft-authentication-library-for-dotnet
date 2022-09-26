// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.TelemetryCore.TelemetryClient
{
    internal class MsalTelemetryEventDetails : TelemetryEventDetails
    {
        public MsalTelemetryEventDetails()
        {
            Name = TelemetryConstants.AcquireTokenEventName;
        }
    }
}
