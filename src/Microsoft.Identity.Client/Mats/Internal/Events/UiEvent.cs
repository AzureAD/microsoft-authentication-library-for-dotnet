//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

namespace Microsoft.Identity.Client.Mats.Internal.Events
{
    internal class UiEvent : EventBase
    {
        public const string UserCancelledKey = EventNamePrefix + "user_cancelled";

        public const string AccessDeniedKey = EventNamePrefix + "access_denied";

        public UiEvent(string telemetryCorrelationId) : base(EventNamePrefix + "ui_event", telemetryCorrelationId) { }

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
