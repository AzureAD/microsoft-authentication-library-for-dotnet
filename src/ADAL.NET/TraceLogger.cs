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

using System.Diagnostics;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Common;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class TraceLogger : ILogger
    {
        public void Verbose(CallState callState, string format, params object[] args)
        {
            // TODO: This is temporary code. Replace it with correct implementation for Verbose level
            Trace.TraceInformation(PrepareLogMessage(callState, format, args));
        }

        public void Information(CallState callState, string format, params object[] args)
        {
            Trace.TraceInformation(PrepareLogMessage(callState, format, args));
        }

        public void Warning(CallState callState, string format, params object[] args)
        {
            Trace.TraceWarning(PrepareLogMessage(callState, format, args));
        }

        public void Error(CallState callState, string format, params object[] args)
        {
            Trace.TraceError(PrepareLogMessage(callState, format, args));
        }

        internal string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }
    }
}