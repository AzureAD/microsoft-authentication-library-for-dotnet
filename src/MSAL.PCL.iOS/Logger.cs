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
using System.Diagnostics.Tracing;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class Logger : LoggerBase
    {
        internal override void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string message = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), ex.ToString());
            Console.WriteLine(message); //Console.writeline writes to NSLog by default
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Error, message);
        }

        internal override void Verbose(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Verbose, updatedMessage);
        }

        internal override void Information(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Information, updatedMessage);
        }

        internal override void Warning(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Warning, updatedMessage);
        }
    }
}

