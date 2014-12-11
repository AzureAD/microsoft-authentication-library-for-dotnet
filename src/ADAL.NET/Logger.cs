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

using System;
using System.Diagnostics;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger
    {   
        internal static void Verbose(CallState callState, string format, params object[] args)
        {

            string message = PrepareLogMessage(callState, GetCallerType(), format, args);
            AdalTrace.TraceSource.TraceEvent(TraceEventType.Verbose, 1, message);
            if (AdalTrace.LegacyTraceSwitch.TraceVerbose)
            {
                // There is no TraceVerbose method.
                Trace.TraceInformation(message);
            }
        }

        internal static void Information(CallState callState, string format, params object[] args)
        {
            string message = PrepareLogMessage(callState, GetCallerType(), format, args);
            AdalTrace.TraceSource.TraceData(TraceEventType.Information, 2, message);
            if (AdalTrace.LegacyTraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(message);
            }
        }

        internal static void Warning(CallState callState, string format, params object[] args)
        {
            string message = PrepareLogMessage(callState, GetCallerType(), format, args);
            AdalTrace.TraceSource.TraceEvent(TraceEventType.Warning, 3, message);
            if (AdalTrace.LegacyTraceSwitch.TraceWarning)
            {
                Trace.TraceWarning(message);
            }
        }

        internal static void Error(CallState callState, Exception ex)
        {
            string message = PrepareLogMessage(callState, GetCallerType(), "{0}", ex);
            AdalTrace.TraceSource.TraceEvent(TraceEventType.Error, 4, message);
            if (AdalTrace.LegacyTraceSwitch.TraceError)
            {
                Trace.TraceError(message);
            }
        }

        private static string GetCallerType()
        {
            StackFrame frame = new StackFrame(2, false);
            var method = frame.GetMethod();
            return (method.ReflectedType != null) ? method.ReflectedType.Name : null;
        }
    }
}