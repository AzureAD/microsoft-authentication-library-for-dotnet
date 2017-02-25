//------------------------------------------------------------------------------
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
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class Logger : LoggerBase
    {
        static Logger()
        {
            MsalEventSource = new MsalEventSource();
        }

        internal static MsalEventSource MsalEventSource { get; }

        internal override void Error(RequestContext requestContext, string errorMessage, string callerFilePath = "")
        {
            string message = PrepareLogMessage(requestContext, GetCallerFilename(callerFilePath), errorMessage);
            MsalEventSource.Error(message);
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Error, message);
        }

        internal override void Error(RequestContext requestContext, Exception ex,
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string log = PrepareLogMessage(requestContext, GetCallerFilename(callerFilePath), ex.ToString());
            MsalEventSource.Error(log);
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Error, log);
        }

        internal override void Verbose(RequestContext requestContext, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string log = PrepareLogMessage(requestContext, GetCallerFilename(callerFilePath), message);
            MsalEventSource.Verbose(log);
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Verbose, log);
        }

        internal override void Information(RequestContext requestContext, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string log = PrepareLogMessage(requestContext, GetCallerFilename(callerFilePath), message);
            MsalEventSource.Information(log);
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Information, log);
        }

        internal override void Warning(RequestContext requestContext, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            string log = PrepareLogMessage(requestContext, GetCallerFilename(callerFilePath), message);
            MsalEventSource.Warning(log);
            LoggerCallbackHandler.ExecuteCallback(LogLevel.Warning, log);
        }
    }
}