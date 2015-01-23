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
using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class Logger : LoggerBase
    {
        static Logger()
        {
            AdalEventSource = new AdalEventSource();
        }

        internal static AdalEventSource AdalEventSource { get; private set; }

        internal override void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.Error(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), ex.ToString()));
        }

        internal override void Verbose(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.Verbose(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message));
        }

        internal override void Information(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.Information(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message));
        }

        internal override void Warning(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.Warning(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message));
        }

        private static string GetCallerFilename(string callerFilePath)
        {
            return callerFilePath.Substring(callerFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        }

        internal static string PrepareLogMessage(CallState callState, string classOrComponent, string message)
        {
            string correlationId = (callState != null) ? callState.CorrelationId.ToString() : string.Empty;
            return string.Format(CultureInfo.CurrentCulture, "{0}: {1} - {2}: {3}", DateTime.UtcNow, correlationId, classOrComponent, message);
        }
    }
}