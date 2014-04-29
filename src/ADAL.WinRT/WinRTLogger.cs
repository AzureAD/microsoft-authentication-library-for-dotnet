//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Common;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WinRTLogger : EventSource, ILogger
    {
        public void Verbose(CallState callState, string format, params object[] args)
        {
            this.Verbose(PrepareLogMessage(callState, format, args));
        }

        public void Information(CallState callState, string format, params object[] args)
        {
            this.Information(PrepareLogMessage(callState, format, args));
        }

        public void Warning(CallState callState, string format, params object[] args)
        {
            this.Warning(PrepareLogMessage(callState, format, args));
        }

        public void Error(CallState callState, string format, params object[] args)
        {
            this.Error(PrepareLogMessage(callState, format, args));
        }

        internal string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }

        [Event(1, Level = EventLevel.Verbose)]
        private void Verbose(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        private void Information(string message)
        {
            this.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        private void Warning(string message)
        {
            this.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        private void Error(string message)
        {
            this.WriteEvent(4, message);
        }

    }
}