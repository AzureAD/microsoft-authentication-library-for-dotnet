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
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal abstract class LoggerBase
    {
        internal abstract void DefaultLog(LogLevel logLevel, string message);

        private void Log(CallState callState, LogLevel logLevel, string message, bool containsPii,
            [CallerFilePath] string callerFilePath = "")
        {
            if (!LoggerCallbackHandler.PiiLoggingEnabled && containsPii)
            {
                return;
            }

            var formattedMessage = FormatLogMessage(callState, GetCallerFilename(callerFilePath), message);

            if (LoggerCallbackHandler.UseDefaultLogging && !containsPii)
            {
                DefaultLog(logLevel, formattedMessage);
            }

            if (LoggerCallbackHandler.LogCallback != null)
            {
                LoggerCallbackHandler.ExecuteCallback(logLevel, formattedMessage, containsPii);
            }
            else if (!containsPii)
            {
                // execute obsolete IAdalLogCallback only if LogCallback is not set and message does not contain Pii
                LoggerCallbackHandler.ExecuteObsoleteCallback(logLevel, formattedMessage);
            }
        }

        internal static string GetCallerFilename(string callerFilePath)
        {
            return callerFilePath.Substring(callerFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        }

        internal string CorrelationId { get; set; } = string.Empty;

        internal string FormatLogMessage(CallState callState, string classOrComponent, string message)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:O}: {1} - {2}: {3}", DateTime.UtcNow, CorrelationId, classOrComponent, message);
        }

        internal void Verbose(CallState callState, string message)
        {
            Log(callState, LogLevel.Verbose, message, false);
        }

        internal void VerbosePii(CallState callState, string message)
        {
            Log(callState, LogLevel.Verbose, message, true);
        }

        internal void Information(CallState callState, string message)
        {
            Log(callState, LogLevel.Information, message, false);
        }

        internal void InformationPii(CallState callState, string message)
        {
            Log(callState, LogLevel.Information, message, true);
        }

        internal void Warning(CallState callState, string message)
        {
            Log(callState, LogLevel.Warning, message, false);
        }

        internal void WarningPii(CallState callState, string message)
        {
            Log(callState, LogLevel.Warning, message, true);
        }

        internal void Error(CallState callState, Exception ex)
        {
            Log(callState, LogLevel.Error, ex.GetPiiScrubbedDetails(), false);
        }

        internal void ErrorPii(CallState callState, Exception ex)
        {
            Log(callState, LogLevel.Error, ex.ToString(), true);
        }

        internal void Error(CallState callState, string message)
        {
            Log(callState, LogLevel.Error, message, false);
        }
    }
}
