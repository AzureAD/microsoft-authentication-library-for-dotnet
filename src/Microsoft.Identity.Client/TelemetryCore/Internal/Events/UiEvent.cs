// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal class UiEvent : EventBase
    {
        public const string UserCancelledKey = EventNamePrefix + "user_cancelled";

        public const string AccessDeniedKey = EventNamePrefix + "access_denied";

        public UiEvent(string correlationId) : base(EventNamePrefix + "ui_event", correlationId) { }

        public bool UserCancelled
        {
#pragma warning disable CA1305 // .net standard does not have an overload for this
            set { this[UserCancelledKey] = value.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        public bool AccessDenied
        {
#pragma warning disable CA1305 // .net standard does not have an overload for this
            set { this[AccessDeniedKey] = value.ToString().ToLowerInvariant(); }
#pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}
