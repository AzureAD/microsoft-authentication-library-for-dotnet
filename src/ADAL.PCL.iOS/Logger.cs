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

using System;
using System.Diagnostics.Tracing;
﻿
namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class Logger : LoggerBase
    {
        internal override void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string message = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), ex.ToString());
            if (LoggerCallbackHandler.UseDefaultLogging)
            {
                Console.WriteLine(message); //Console.writeline writes to NSLog by default
            }

            LoggerCallbackHandler.ExecuteCallback(LogLevel.Error, message);
        }

        internal override void Verbose(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            if (LoggerCallbackHandler.UseDefaultLogging)
            {
                Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            }

            LoggerCallbackHandler.ExecuteCallback(LogLevel.Verbose, updatedMessage);
        }

        internal override void Information(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            if (LoggerCallbackHandler.UseDefaultLogging)
            {
                Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            }

            LoggerCallbackHandler.ExecuteCallback(LogLevel.Information, updatedMessage);
        }

        internal override void Warning(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string updatedMessage = PrepareLogMessage(callState, GetCallerFilename(callerFilePath), message);
            if (LoggerCallbackHandler.UseDefaultLogging)
            {
                Console.WriteLine(updatedMessage); //Console.writeline writes to NSLog by default
            }

            LoggerCallbackHandler.ExecuteCallback(LogLevel.Warning, updatedMessage);
        }
    }
}

